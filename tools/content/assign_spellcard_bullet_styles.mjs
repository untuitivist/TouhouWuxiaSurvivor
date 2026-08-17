import { readdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";

const assignments = new Map([
  ["reimu_fantasy_seal", "orb"],
  ["reimu_evil_sealing_circle", "amulet"],
  ["reimu_duplex_barrier", "amulet"],
  ["reimu_omnidirectional_oni_binding_circle", "amulet"],
  ["marisa_master_spark", "laser"],
  ["marisa_stardust_reverie", "star"],
  ["th01_shingyoku_yinyang_trinity", "orb"],
  ["th01_sariel_fallen_judgement", "laser"],
  ["th02_rika_evil_eye_sigma", "large_orb"],
  ["th02_mima_vengeful_spirit_cannon", "laser"],
  ["th03_chiyuri_probability_ship", "shard"],
  ["th03_yumemi_science_magic_cannon", "laser"],
  ["th04_yuuka_gensokyo_flower", "butterfly"],
  ["th04_gengetsu_double_magic_cannon", "laser"],
  ["th05_yumeko_pandemonium_sword_array", "knife"],
  ["th05_shinki_makai_creation", "amulet"],
  ["th06_rumia_night_bird", "shard"],
  ["th06_cirno_perfect_freeze", "shard"],
  ["th06_meiling_rainbow_wind_chime", "orb"],
  ["th06_patchouli_philosophers_stone", "large_orb"],
  ["th06_sakuya_killing_doll", "knife"],
  ["th06_remilia_scarlet_shoot", "needle"],
  ["th06_flandre_laevatein", "flame"],
  ["th07_yuyuko_ink_black_cherry", "butterfly"],
  ["th07_yukari_boundary_of_life_and_death", "amulet"],
  ["th08_kaguya_hourai_jeweled_branch", "shard"],
  ["th08_mokou_fujiyama_volcano", "flame"],
  ["th09_yuuka_gensokyo_bloom", "butterfly"],
  ["th09_eiki_last_judgement", "laser"],
  ["th10_kanako_expanded_onbashira", "needle"],
  ["th10_suwako_mishaguji", "amulet"],
  ["th11_utsuho_petaflare", "flame"],
  ["th11_koishi_subterranean_rose", "orb"],
  ["th12_byakuren_majin_recitation", "laser"],
  ["th12_nue_barrage_chimera", "butterfly"],
  ["th13_miko_prince_shotoku_oparts", "amulet"],
  ["th13_mamizou_wild_deserted_island", "large_orb"],
  ["th14_shinmyoumaru_grow_bigger", "large_orb"],
  ["th14_raiko_pristine_beat", "star"],
  ["th15_junko_pure_bullet_hell", "orb"],
  ["th15_hecatia_trinitarian_rhapsody", "orb"],
  ["th16_okina_secret_god_backlight", "amulet"],
  ["th16_okina_scorch_by_hot_summer", "flame"],
  ["th17_keiki_haniwa_creation", "amulet"],
  ["th17_saki_black_pegasus_meteor_shot", "needle"],
  ["th18_chimata_ownerless_offering", "amulet"],
  ["th18_momoyo_cannibalistic_insect", "butterfly"],
  ["th19_hisami_prisonbreak_stalker", "needle"],
  ["th19_zanmu_lost_sheep_kingdom", "amulet"],
  ["th20_ariya_barrage_fossil", "shard"],
  ["th20_beiko_tartarian_rhapsody", "orb"],
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

/** 读取并校验每张奥义的显式弹型；写入模式只增加该字段，不改动战斗数据。 */
async function updateManifest(filePath, writeChanges, seen) {
  const original = await readFile(filePath, "utf8");
  if (original.charCodeAt(0) === 0xfeff) {
    throw new Error(`${filePath}: UTF-8 BOM is forbidden.`);
  }

  const manifest = JSON.parse(original);
  for (const card of manifest.spellcards ?? []) {
    const style = assignments.get(card.id);
    if (!style) throw new Error(`${filePath}: missing bullet style for ${card.id}.`);
    if (card.bullet_style !== undefined && card.bullet_style !== style) {
      throw new Error(`${filePath}: ${card.id} expected ${style}, got ${card.bullet_style}.`);
    }
    if (writeChanges) card.bullet_style = style;
    else if (card.bullet_style === undefined) {
      throw new Error(`${filePath}: ${card.id} has not been migrated; run with --write.`);
    }
    seen.add(card.id);
  }

  if (writeChanges) {
    await writeFile(filePath, `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
  }
}

/** 默认只审计，显式 --write 才更新清单，并报告十类弹型的实际覆盖分布。 */
async function main() {
  const writeChanges = process.argv.includes("--write");
  const seen = new Set();
  for (const filePath of await manifestPaths()) {
    await updateManifest(filePath, writeChanges, seen);
  }
  const missing = [...assignments.keys()].filter((id) => !seen.has(id));
  if (seen.size !== assignments.size || missing.length > 0) {
    throw new Error(`Expected ${assignments.size} cards, saw ${seen.size}; missing ${missing}.`);
  }
  const counts = [...assignments.values()].reduce((result, style) => {
    result[style] = (result[style] ?? 0) + 1;
    return result;
  }, {});
  console.log(`spell bullet styles ok: ${seen.size} cards; ${JSON.stringify(counts)}`);
}

await main();
