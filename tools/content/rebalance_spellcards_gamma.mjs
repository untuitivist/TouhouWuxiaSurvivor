import { readdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";

const balanceVersion = 2;
// 必须与 SpellCardContributionModel 保持逐项一致；C# 契约测试负责验证运行投影，工具负责审计 JSON。
const contributionPolicy = Object.freeze({
  areaEdgeDamageMultiplier: 0.45,
  guardDefenseCreditWeight: 0.17,
  delivery: Object.freeze({
    homing_volley: 0.9,
    focused_volley: 1.0,
    area_burst: 0.85,
    guard_field: 0.72,
  }),
  activation: Object.freeze({
    periodic: 1.0,
    crowd: 0.9,
    on_damaged: 0.58,
  }),
});
const effectRules = Object.freeze({
  homing_volley: Object.freeze({
    oldDamage: [7, 12], damage: [0.65, 1.0], target: [0.75, 1.15],
    budget: [0.35, 0.85],
  }),
  focused_volley: Object.freeze({
    oldDamage: [12, 18], damage: [1.1, 1.55], target: [0.45, 0.6],
    budget: [0.34, 0.76],
  }),
  area_burst: Object.freeze({
    oldDamage: [9, 18], damage: [1.313, 2.013], target: [0.8, 1.1],
    budget: [0.29, 0.68],
  }),
  guard_field: Object.freeze({
    oldDamage: [6, 14], damage: [0.55, 0.85], target: [0.55, 0.8],
    budget: [0.16, 0.27],
  }),
});

/**
 * 将浮点结果收敛到三位小数，使策划表可读且重复执行不会产生二进制长尾。
 */
function rounded(value) {
  return Number(value.toFixed(3));
}

/**
 * 把旧区间内的身份差异线性投影到新倍率区间；异常旧值会钳制而不是继续放大。
 */
function remap(value, oldRange, newRange) {
  const ratio = Math.max(0, Math.min(1,
    (value - oldRange[0]) / (oldRange[1] - oldRange[0])));
  return rounded(newRange[0] + ratio * (newRange[1] - newRange[0]));
}

/**
 * 按投射语义生成显式目标倍率：范围与护身不再依赖零值代表无限目标的隐式约定。
 */
function targetScale(card, rule) {
  if (card.effect === "homing_volley") {
    return remap(card.target_scale, [1.0, 2.0], rule.target);
  }
  if (card.effect === "focused_volley") {
    return remap(card.target_scale, [0.5, 1.0], rule.target);
  }
  const damageRatio = Math.max(0, Math.min(1,
    (card.damage_scale - rule.oldDamage[0]) /
      (rule.oldDamage[1] - rule.oldDamage[0])));
  return rounded(rule.target[1] - damageRatio *
    (rule.target[1] - rule.target[0]));
}

/**
 * 迁移单张旧倍率卡；伤害较高的卡保留身份优势，但通过目标数和原有周期偿还预算。
 */
function rebalanceLegacyCard(card) {
  const rule = effectRules[card.effect];
  if (!rule) {
    throw new Error(`Unknown spell effect: ${card.effect}`);
  }
  return {
    ...card,
    damage_scale: remap(card.damage_scale, rule.oldDamage,
      card.effect === "area_burst" ? [0.75, 1.15] : rule.damage),
    target_scale: targetScale(card, rule),
  };
}

/**
 * 把 v1 范围奥义迁移到主攻公共预算；目标上限与边缘衰减已计价，因此只调整伤害维度。
 */
function rebalanceAreaBudget(card) {
  if (card.effect !== "area_burst") {
    return card;
  }
  return {
    ...card,
    damage_scale: remap(card.damage_scale, [0.75, 1.15],
      effectRules.area_burst.damage),
  };
}

/**
 * 按清单声明版本依次执行迁移；禁止跳过未知未来版本或重复放大已经迁移的倍率。
 */
function migrateCard(card, sourceVersion) {
  let migrated = card;
  if (sourceVersion < 1) {
    migrated = rebalanceLegacyCard(migrated);
  }
  if (sourceVersion < 2) {
    migrated = rebalanceAreaBudget(migrated);
  }
  return migrated;
}

/**
 * 计算跨效果可比较的持续贡献预算；权重折算命中可靠度、触发可用率与护身收益。
 */
function budgetScore(card) {
  const deliveryWeight = contributionPolicy.delivery[card.effect];
  const activationWeight = contributionPolicy.activation[card.activation];
  const areaExpectedMultiplier =
    (1 + 2 * contributionPolicy.areaEdgeDamageMultiplier) / 3;
  const targetDamageMultiplier =
    card.effect === "area_burst" || card.effect === "guard_field"
      ? areaExpectedMultiplier
      : 1;
  const offense = card.damage_scale * card.target_scale /
    card.interval_scale * deliveryWeight * targetDamageMultiplier *
    activationWeight;
  const defense = card.effect === "guard_field"
    ? card.defense_scale / card.interval_scale *
      contributionPolicy.guardDefenseCreditWeight * activationWeight
    : 0;
  return offense + defense;
}

/**
 * 验证贡献口径本身完整且范围期望积分为 (1+2m)/3，防止审计脚本静默漏掉新效果或触发类型。
 */
function verifyContributionPolicy() {
  const effectKeys = Object.keys(effectRules);
  if (effectKeys.some((effect) =>
      !Number.isFinite(contributionPolicy.delivery[effect]))) {
    throw new Error("Contribution policy is missing an effect delivery weight.");
  }
  const activationKeys = ["periodic", "crowd", "on_damaged"];
  if (activationKeys.some((activation) =>
      !Number.isFinite(contributionPolicy.activation[activation]))) {
    throw new Error("Contribution policy is missing an activation availability.");
  }
  const expected = (1 + 2 * contributionPolicy.areaEdgeDamageMultiplier) / 3;
  if (Math.abs(expected - 0.6333333333333333) > 1e-12) {
    throw new Error("Area contribution expectation no longer matches the 45% edge policy.");
  }
}

/**
 * 审计单卡所有数值维度、效果专属区间和综合预算，错误消息包含可直接定位的卡牌编号。
 */
function verifyCard(card, sourcePath) {
  const rule = effectRules[card.effect];
  if (!rule) {
    throw new Error(`${sourcePath}: ${card.id} has an unknown effect.`);
  }
  const values = [
    card.interval_scale, card.range_scale, card.damage_scale, card.target_scale,
    card.activation_threshold_scale, card.defense_scale,
    card.projectile_speed_scale, card.impact_range_scale,
    card.travel_duration_scale, card.spawn_distance_scale,
  ];
  if (values.some((value) => !Number.isFinite(value)) ||
      card.interval_scale <= 0 || card.range_scale <= 0 ||
      card.damage_scale <= 0 || card.target_scale <= 0) {
    throw new Error(`${sourcePath}: ${card.id} has invalid finite scales.`);
  }
  const inRange = (value, range) =>
    value >= range[0] - 0.0001 && value <= range[1] + 0.0001;
  if (!inRange(card.damage_scale, rule.damage) ||
      !inRange(card.target_scale, rule.target)) {
    throw new Error(`${sourcePath}: ${card.id} violates ${card.effect} ranges.`);
  }
  if (card.effect === "guard_field" && card.defense_scale <= 0) {
    throw new Error(`${sourcePath}: ${card.id} has no guard duration.`);
  }
  if (card.effect !== "guard_field" && card.defense_scale !== 0) {
    throw new Error(`${sourcePath}: ${card.id} leaks guard duration.`);
  }
  const score = budgetScore(card);
  if (!inRange(score, rule.budget)) {
    throw new Error(`${sourcePath}: ${card.id} budget ${rounded(score)} is outside ` +
      `${rule.budget[0]}..${rule.budget[1]}.`);
  }
  return score;
}

/**
 * 读取单份清单并检查无 BOM；写入模式只迁移未标记版本，随后总是执行完整预算审计。
 */
async function processManifest(filePath, writeChanges, previewChanges) {
  const originalText = await readFile(filePath, "utf8");
  if (originalText.charCodeAt(0) === 0xfeff) {
    throw new Error(`${filePath}: UTF-8 BOM is forbidden.`);
  }
  const manifest = JSON.parse(originalText);
  if (!Array.isArray(manifest.spellcards)) {
    return { cards: 0, entries: [] };
  }
  const sourceVersion = manifest.spellcard_balance_version ?? 0;
  if (!Number.isInteger(sourceVersion) || sourceVersion < 0 ||
      sourceVersion > balanceVersion) {
    throw new Error(`${filePath}: unsupported spell-card balance version ${sourceVersion}.`);
  }
  if (sourceVersion !== balanceVersion &&
      !writeChanges && !previewChanges) {
    throw new Error(`${filePath}: run this tool with --write to apply balance v2.`);
  }
  if (sourceVersion !== balanceVersion) {
    manifest.spellcards = manifest.spellcards.map((card) =>
      migrateCard(card, sourceVersion));
    manifest.spellcard_balance_version = balanceVersion;
  }
  const entries = manifest.spellcards.map((card) => ({
    effect: card.effect,
    score: verifyCard(card, filePath),
  }));
  if (writeChanges) {
    await writeFile(filePath, `${JSON.stringify(manifest, null, 2)}\n`,
      { encoding: "utf8" });
  }
  return { cards: manifest.spellcards.length, entries };
}

/**
 * 枚举本体与二十个正作清单，保证移动卡牌后仍以实际归属审计总计四十六张奥义。
 */
async function manifestPaths() {
  const packsRoot = path.resolve("content", "packs");
  const directories = (await readdir(packsRoot, { withFileTypes: true }))
    .filter((entry) => entry.isDirectory())
    .sort((left, right) => left.name.localeCompare(right.name));
  if (directories.length !== 20) {
    throw new Error(`Expected 20 official packs, got ${directories.length}.`);
  }
  return [
    path.resolve("content", "base", "pack.json"),
    ...directories.map((entry) =>
      path.join(packsRoot, entry.name, "pack.json")),
  ];
}

/**
 * 汇总各效果预算的最小值、均值和最大值，使新增内容的横向强度漂移可直接人工复核。
 */
function summarize(entries) {
  return Object.keys(effectRules).map((effect) => {
    const scores = entries.filter((entry) => entry.effect === effect)
      .map((entry) => entry.score);
    const average = scores.reduce((sum, value) => sum + value, 0) / scores.length;
    return `${effect}=${rounded(Math.min(...scores))}/` +
      `${rounded(average)}/${rounded(Math.max(...scores))}`;
  }).join(", ");
}

/**
 * 默认执行只读审计；显式 `--write` 才写入 UTF-8 without BOM，并再次报告全目录预算范围。
 */
async function main() {
  verifyContributionPolicy();
  const writeChanges = process.argv.includes("--write");
  const previewChanges = process.argv.includes("--preview");
  let cardCount = 0;
  const entries = [];
  for (const filePath of await manifestPaths()) {
    const result = await processManifest(filePath, writeChanges, previewChanges);
    cardCount += result.cards;
    entries.push(...result.entries);
  }
  if (cardCount !== 51) {
    throw new Error(`Expected 51 spell cards, got ${cardCount}.`);
  }
  console.log(`spellcard-balance-v2 ok: 20 packs + base, ${cardCount} cards, ` +
    `budget ${rounded(Math.min(...entries.map((entry) => entry.score)))}..` +
    `${rounded(Math.max(...entries.map((entry) => entry.score)))}`);
  console.log(`effect budget min/avg/max: ${summarize(entries)}`);
  console.log(`contribution policy: edge=${contributionPolicy.areaEdgeDamageMultiplier}, ` +
    `area_expected=${rounded((1 + 2 * contributionPolicy.areaEdgeDamageMultiplier) / 3)}, ` +
    `guard_credit=${contributionPolicy.guardDefenseCreditWeight}`);
}

await main();
