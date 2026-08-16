import { readdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";

const schemaVersion = 2;
const reference = Object.freeze({
  intervalSeconds: 5.25,
  targetRange: 460,
  attackPower: 1,
  targetCapacity: 6,
  defenseSeconds: 1,
  projectileSpeed: 360,
  impactRange: 12,
  travelSeconds: 2,
  spawnDistance: 18,
  legacyProjectileSpeed: 440,
  legacySpawnDistance: 24,
});

/**
 * 将数值收敛到足以无损回环旧战斗参数的小数精度，避免 JSON 出现二进制浮点长尾。
 */
function rounded(value) {
  return Number(value.toFixed(9));
}

/**
 * 把旧战况触发文案迁成纯周期语义；锁敌、群攻等目标描述保留为效果而不是施展门槛。
 */
function normalizeDescription(description, activation) {
  const body = description
    .replaceAll("八枚", "多枚")
    .replace(/^(灵力满溢时|灵力充盈时|周天运转时|周天一转，|周天就绪且敌群进入范围时|周天就绪且受到伤害时|敌群聚集时|敌群逼近时|强敌出现时|强敌现身时|强敌逼近时|遭受围攻时|危急时)/, "")
    .replace(/^，/, "");
  const prefix = activation === "crowd"
    ? "周天就绪且敌群进入范围时，"
    : activation === "on_damaged"
      ? "周天就绪且受到伤害时，"
      : "周天一转，";
  return `${prefix}${body}`;
}

/**
 * 把一张 v1 奥义的最终绝对值转换为相对于参考角色属性的 v2 系数，并移除充能与战况触发字段。
 */
function inferActivation(card) {
  if (card.activation) {
    return card.activation;
  }
  const damagedAreaCards = new Set([
    "th08_mokou_fujiyama_volcano",
    "th11_utsuho_petaflare",
    "th16_okina_scorch_by_hot_summer",
  ]);
  if (card.trigger === "danger" || card.effect === "guard_field" ||
      damagedAreaCards.has(card.id)) {
    return "on_damaged";
  }
  return card.trigger === "crowd" || card.effect !== "focused_volley"
    ? "crowd"
    : "periodic";
}

/**
 * 迁移单张奥义，同时保留定时、敌群和受击三类自动运转语义，但彻底移除资源费用。
 */
function migrateCard(card) {
  if ("interval_scale" in card) {
    const activation = inferActivation(card);
    return {
      ...card,
      activation,
      activation_threshold_scale: card.activation_threshold_scale ?? 0.5,
      description: normalizeDescription(card.description, activation),
    };
  }

  const migrated = { ...card };
  migrated.activation = inferActivation(card);
  migrated.activation_threshold_scale = 0.5;
  delete migrated.trigger;
  delete migrated.power_cost;
  delete migrated.cooldown_seconds;
  delete migrated.effect_range;
  delete migrated.damage;
  delete migrated.target_count;
  delete migrated.defense_seconds;
  migrated.description = normalizeDescription(migrated.description, migrated.activation);
  migrated.interval_scale = rounded(card.cooldown_seconds / reference.intervalSeconds);
  migrated.range_scale = rounded(card.effect_range / reference.targetRange);
  migrated.damage_scale = rounded(card.damage / reference.attackPower);
  migrated.target_scale = rounded(card.target_count / reference.targetCapacity);
  migrated.defense_scale = rounded(card.defense_seconds / reference.defenseSeconds);
  migrated.projectile_speed_scale = rounded(
    reference.legacyProjectileSpeed / reference.projectileSpeed);
  migrated.impact_range_scale = card.target_count > 0
    ? rounded(reference.impactRange / card.effect_range)
    : 0;
  migrated.travel_duration_scale = rounded(
    reference.travelSeconds * reference.legacyProjectileSpeed / card.effect_range);
  migrated.spawn_distance_scale = rounded(
    reference.legacySpawnDistance / reference.spawnDistance);
  return migrated;
}

/**
 * 严格验证 v2 卡片没有混入旧充能/绝对值字段，且全部系数均为有限数值并满足零值语义。
 */
function verifyV2Card(card, sourcePath) {
  const legacyFields = [
    "trigger", "power_cost", "cooldown_seconds", "effect_range",
    "damage", "target_count", "defense_seconds",
  ];
  const scaleFields = [
    "interval_scale", "range_scale", "damage_scale", "target_scale",
    "activation_threshold_scale",
    "defense_scale", "projectile_speed_scale", "impact_range_scale",
    "travel_duration_scale", "spawn_distance_scale",
  ];
  if (!["periodic", "crowd", "on_damaged"].includes(card.activation)) {
    throw new Error(`${sourcePath}: ${card.id} has invalid activation.`);
  }
  for (const legacy of legacyFields) {
    if (legacy in card) {
      throw new Error(`${sourcePath}: ${card.id} retains legacy field ${legacy}.`);
    }
  }
  for (const field of scaleFields) {
    if (!Number.isFinite(card[field])) {
      throw new Error(`${sourcePath}: ${card.id} has invalid ${field}.`);
    }
  }
  if (card.interval_scale <= 0 || card.range_scale <= 0 ||
      card.damage_scale <= 0 || card.projectile_speed_scale <= 0 ||
      card.activation_threshold_scale <= 0 ||
      card.travel_duration_scale <= 0 || card.spawn_distance_scale <= 0 ||
      card.target_scale < 0 || card.defense_scale < 0 || card.impact_range_scale < 0) {
    throw new Error(`${sourcePath}: ${card.id} violates scale bounds.`);
  }
}

/**
 * 以统一参考属性还原 v2 系数，确认迁移没有改变旧卡的周期、范围、伤害、目标数和护身时间。
 */
function verifyRoundTrip(before, after, sourcePath) {
  const checks = [
    ["cooldown", after.interval_scale * reference.intervalSeconds, before.cooldown_seconds],
    ["range", after.range_scale * reference.targetRange, before.effect_range],
    ["damage", after.damage_scale * reference.attackPower, before.damage],
    ["targets", after.target_scale * reference.targetCapacity, before.target_count],
    ["defense", after.defense_scale * reference.defenseSeconds, before.defense_seconds],
  ];
  for (const [name, actual, expected] of checks) {
    if (Math.abs(actual - expected) > 0.001) {
      throw new Error(`${sourcePath}: ${before.id} failed ${name} round-trip.`);
    }
  }
}

/**
 * 迁移或校验单个内容包；写入始终使用 UTF-8 without BOM，并保持两空格仓库格式。
 */
async function processPack(filePath, writeChanges) {
  const originalText = await readFile(filePath, "utf8");
  if (originalText.charCodeAt(0) === 0xfeff) {
    throw new Error(`${filePath}: UTF-8 BOM is forbidden.`);
  }

  const pack = JSON.parse(originalText);
  if (!Array.isArray(pack.spellcards)) {
    return 0;
  }

  const originalCards = pack.spellcards;
  const migratedCards = originalCards.map(migrateCard);
  for (let index = 0; index < originalCards.length; index += 1) {
    if (!("interval_scale" in originalCards[index])) {
      verifyRoundTrip(originalCards[index], migratedCards[index], filePath);
    }
  }
  for (const card of migratedCards) {
    verifyV2Card(card, filePath);
  }

  const migratedPack = {};
  for (const [key, value] of Object.entries(pack)) {
    if (key === "additions") {
      migratedPack.spellcard_schema_version = schemaVersion;
    }
    migratedPack[key] = key === "spellcards" ? migratedCards : value;
  }
  if (!("spellcard_schema_version" in migratedPack)) {
    migratedPack.spellcard_schema_version = schemaVersion;
  }

  const output = `${JSON.stringify(migratedPack, null, 2)}\n`;
  if (writeChanges) {
    await writeFile(filePath, output, { encoding: "utf8" });
  } else if (originalText !== output) {
    throw new Error(`${filePath}: schema v2 migration is not applied.`);
  }
  return migratedCards.length;
}

/**
 * 枚举本体和全部官方包并强制 20 包、51 卡契约；`--write` 执行迁移，默认只做仓库一致性检查。
 */
async function main() {
  const writeChanges = process.argv.includes("--write");
  const packsRoot = path.resolve("content", "packs");
  const directories = (await readdir(packsRoot, { withFileTypes: true }))
    .filter((entry) => entry.isDirectory())
    .sort((left, right) => left.name.localeCompare(right.name));
  if (directories.length !== 20) {
    throw new Error(`Expected 20 official packs, got ${directories.length}.`);
  }
  const manifestPaths = [
    path.resolve("content", "base", "pack.json"),
    ...directories.map((directory) =>
      path.join(packsRoot, directory.name, "pack.json")),
  ];
  let cardCount = 0;
  for (const manifestPath of manifestPaths) {
    cardCount += await processPack(manifestPath, writeChanges);
  }
  if (cardCount !== 51) {
    throw new Error(`Expected 51 cards across base and 20 packs, got ${cardCount}.`);
  }
  console.log(`spellcard-schema-v2 ok: base + ${directories.length} packs, ` +
    `${cardCount} cards`);
}

await main();
