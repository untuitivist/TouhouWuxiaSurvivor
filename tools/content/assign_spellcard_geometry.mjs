import { readdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";

const assignments = new Map([
  ["reimu_fantasy_seal", "orbit"],
  ["reimu_evil_sealing_circle", "ring"],
  ["reimu_duplex_barrier", "ring"],
  ["reimu_omnidirectional_oni_binding_circle", "ring"],
  ["marisa_master_spark", "line"],
  ["marisa_stardust_reverie", "fan"],
  ["th01_shingyoku_yinyang_trinity", "orbit"],
  ["th01_sariel_fallen_judgement", "line"],
  ["th02_rika_evil_eye_sigma", "fan"],
  ["th02_mima_vengeful_spirit_cannon", "line"],
  ["th03_chiyuri_probability_ship", "backstab"],
  ["th03_yumemi_science_magic_cannon", "line"],
  ["th04_yuuka_gensokyo_flower", "ring"],
  ["th04_gengetsu_double_magic_cannon", "fan"],
  ["th05_yumeko_pandemonium_sword_array", "fan"],
  ["th05_shinki_makai_creation", "ring"],
  ["th06_rumia_night_bird", "fan"],
  ["th06_cirno_perfect_freeze", "ring"],
  ["th06_meiling_rainbow_wind_chime", "fan"],
  ["th06_patchouli_philosophers_stone", "orbit"],
  ["th06_sakuya_killing_doll", "backstab"],
  ["th06_remilia_scarlet_shoot", "fan"],
  ["th06_flandre_laevatein", "line"],
  ["th07_yuyuko_ink_black_cherry", "ring"],
  ["th07_yukari_boundary_of_life_and_death", "backstab"],
  ["th08_kaguya_hourai_jeweled_branch", "orbit"],
  ["th08_mokou_fujiyama_volcano", "ring"],
  ["th09_yuuka_gensokyo_bloom", "ring"],
  ["th09_eiki_last_judgement", "line"],
  ["th10_kanako_expanded_onbashira", "line"],
  ["th10_suwako_mishaguji", "ring"],
  ["th11_utsuho_petaflare", "ring"],
  ["th11_koishi_subterranean_rose", "backstab"],
  ["th12_byakuren_majin_recitation", "fan"],
  ["th12_nue_barrage_chimera", "fan"],
  ["th13_miko_prince_shotoku_oparts", "orbit"],
  ["th13_mamizou_wild_deserted_island", "backstab"],
  ["th14_shinmyoumaru_grow_bigger", "ring"],
  ["th14_raiko_pristine_beat", "orbit"],
  ["th15_junko_pure_bullet_hell", "ring"],
  ["th15_hecatia_trinitarian_rhapsody", "orbit"],
  ["th16_okina_secret_god_backlight", "backstab"],
  ["th16_okina_scorch_by_hot_summer", "fan"],
  ["th17_keiki_haniwa_creation", "line"],
  ["th17_saki_black_pegasus_meteor_shot", "line"],
  ["th18_chimata_ownerless_offering", "orbit"],
  ["th18_momoyo_cannibalistic_insect", "fan"],
  ["th19_hisami_prisonbreak_stalker", "backstab"],
  ["th19_zanmu_lost_sheep_kingdom", "backstab"],
  ["th20_ariya_barrage_fossil", "ring"],
  ["th20_beiko_tartarian_rhapsody", "fan"],
]);

/** 枚举本体和二十个正作清单，保持固定排序以获得可复现的写入结果。 */
async function manifestPaths() {
  const packRoot = path.resolve("content", "packs");
  const directories = (await readdir(packRoot, { withFileTypes: true }))
    .filter((entry) => entry.isDirectory())
    .sort((left, right) => left.name.localeCompare(right.name));
  return [
    path.resolve("content", "base", "pack.json"),
    ...directories.map((entry) => path.join(packRoot, entry.name, "pack.json")),
  ];
}

/** 读取、检查并按显式策划表补入 geometry 字段，不改变任何现有数值或文本字段。 */
async function updateManifest(filePath, writeChanges, seen) {
  const original = await readFile(filePath, "utf8");
  if (original.charCodeAt(0) === 0xfeff) {
    throw new Error(`${filePath}: UTF-8 BOM is forbidden.`);
  }

  const manifest = JSON.parse(original);
  for (const card of manifest.spellcards ?? []) {
    const geometry = assignments.get(card.id);
    if (!geometry) {
      throw new Error(`${filePath}: missing geometry assignment for ${card.id}.`);
    }
    if (card.geometry !== undefined && card.geometry !== geometry) {
      throw new Error(`${filePath}: ${card.id} expected ${geometry}, got ${card.geometry}.`);
    }
    if (writeChanges) {
      card.geometry = geometry;
    } else if (card.geometry === undefined) {
      throw new Error(`${filePath}: ${card.id} has not been migrated; run with --write.`);
    }
    seen.add(card.id);
  }

  if (writeChanges) {
    await writeFile(filePath, `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
  }
}

/** 默认只审计，显式 --write 才更新全部清单，并报告五类几何的实际覆盖分布。 */
async function main() {
  const writeChanges = process.argv.includes("--write");
  const seen = new Set();
  for (const filePath of await manifestPaths()) {
    await updateManifest(filePath, writeChanges, seen);
  }
  const missing = [...assignments.keys()].filter((id) => !seen.has(id));
  if (seen.size !== 51 || missing.length > 0) {
    throw new Error(`Expected 51 assigned cards, saw ${seen.size}; missing ${missing}.`);
  }
  const counts = [...assignments.values()].reduce((result, geometry) => {
    result[geometry] = (result[geometry] ?? 0) + 1;
    return result;
  }, {});
  console.log(`spell geometry ok: ${seen.size} cards; ${JSON.stringify(counts)}`);
}

await main();
