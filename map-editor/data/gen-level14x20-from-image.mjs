/**
 * 与参考图「逐格数据」一致：10×14 原图嵌入 14×20（居中留白），
 * 羊由两格骨牌覆盖原图中所有「羊格」，鸡为单格（与 gen-farm-level.mjs 同源坐标）。
 * 元素：map-editor/workspace.json 中的两格羊、单格鸡。
 * 运行：node gen-level14x20-from-image.mjs
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.join(__dirname, '..', 'workspace.json');

const MAP_W = 14;
const MAP_H = 20;
const VERSION = 4;
const DIR_ORDER = ['down', 'right', 'up', 'left'];

/** 与 gen-farm-level.mjs 完全一致：顶行 r=0，s=羊格 c=鸡格 */
const ROWS = [
  [
    [0, 5, 's'],
    [0, 6, 's'],
  ],
  [
    [1, 1, 's'],
    [1, 2, 's'],
    [1, 3, 's'],
    [1, 4, 's'],
    [1, 6, 's'],
    [1, 7, 's'],
    [1, 8, 's'],
  ],
  [
    [2, 1, 's'],
    [2, 2, 's'],
    [2, 3, 's'],
    [2, 5, 'c'],
    [2, 6, 's'],
    [2, 7, 's'],
    [2, 8, 's'],
  ],
  [
    [3, 0, 's'],
    [3, 1, 's'],
    [3, 2, 's'],
    [3, 3, 's'],
    [3, 4, 's'],
    [3, 5, 's'],
    [3, 6, 'c'],
    [3, 7, 'c'],
    [3, 8, 's'],
    [3, 9, 's'],
  ],
  [
    [4, 0, 's'],
    [4, 1, 'c'],
    [4, 2, 's'],
    [4, 3, 's'],
    [4, 4, 'c'],
    [4, 5, 's'],
    [4, 6, 's'],
    [4, 7, 's'],
    [4, 8, 's'],
    [4, 9, 's'],
  ],
  [
    [5, 0, 's'],
    [5, 1, 's'],
    [5, 2, 'c'],
    [5, 3, 'c'],
    [5, 4, 'c'],
    [5, 5, 's'],
    [5, 6, 's'],
    [5, 7, 's'],
    [5, 8, 's'],
    [5, 9, 's'],
  ],
  [
    [6, 0, 's'],
    [6, 1, 's'],
    [6, 2, 's'],
    [6, 3, 's'],
    [6, 4, 's'],
    [6, 5, 's'],
    [6, 6, 's'],
    [6, 7, 's'],
    [6, 8, 's'],
    [6, 9, 's'],
  ],
  [
    [7, 1, 's'],
    [7, 2, 's'],
    [7, 3, 's'],
    [7, 4, 's'],
    [7, 5, 's'],
    [7, 6, 's'],
    [7, 7, 's'],
    [7, 8, 's'],
    [7, 9, 's'],
  ],
  [
    [8, 1, 's'],
    [8, 2, 's'],
    [8, 3, 's'],
    [8, 4, 's'],
    [8, 5, 'c'],
    [8, 6, 's'],
    [8, 7, 's'],
    [8, 8, 's'],
    [8, 9, 's'],
  ],
  [
    [9, 1, 's'],
    [9, 2, 's'],
    [9, 3, 's'],
    [9, 4, 's'],
    [9, 5, 'c'],
    [9, 6, 'c'],
    [9, 7, 's'],
    [9, 8, 'c'],
    [9, 9, 's'],
  ],
  [
    [10, 1, 's'],
    [10, 2, 's'],
    [10, 3, 's'],
    [10, 4, 'c'],
    [10, 5, 'c'],
    [10, 6, 'c'],
    [10, 7, 's'],
    [10, 8, 's'],
    [10, 9, 's'],
  ],
  [
    [11, 1, 's'],
    [11, 2, 's'],
    [11, 3, 's'],
    [11, 4, 's'],
    [11, 5, 's'],
    [11, 6, 's'],
    [11, 7, 's'],
    [11, 8, 's'],
    [11, 9, 's'],
  ],
  [
    [12, 1, 's'],
    [12, 2, 's'],
    [12, 3, 's'],
    [12, 4, 's'],
    [12, 5, 's'],
    [12, 6, 's'],
    [12, 7, 's'],
    [12, 8, 's'],
    [12, 9, 's'],
  ],
  [[13, 4, 's']],
];

/** 10×14 嵌入 14×20：左右各 2、上 3 下 3 */
const COL_OFF = 2;
const ROW_OFF = 3;

function key(r, c) {
  return r + ',' + c;
}

function parseKey(k) {
  const [a, b] = k.split(',');
  return [parseInt(a, 10), parseInt(b, 10)];
}

function cloneEl(el) {
  const o = JSON.parse(JSON.stringify(el));
  if (typeof o.hasParam !== 'boolean') o.hasParam = false;
  return o;
}

function getDirection(el) {
  const d = el && el.direction;
  return DIR_ORDER.indexOf(d) >= 0 ? d : 'down';
}

function directionIndex(d) {
  const i = DIR_ORDER.indexOf(d);
  return i >= 0 ? i : 0;
}

function effectivePlacementDirection(el, rotation) {
  const ri = (directionIndex(getDirection(el)) + ((rotation || 0) % 4) + 4) % 4;
  return DIR_ORDER[ri];
}

/** 两相邻羊格 → 顶格 + 旋转（与 app.js 中竖条羊一致：rot0 竖，rot1 横） */
function dominoPlacement(r1, c1, r2, c2) {
  if (r1 === r2) {
    const mc = Math.min(c1, c2);
    return { row: r1, col: mc, rotation: 1 };
  }
  if (c1 === c2) {
    const mr = Math.min(r1, r2);
    return { row: mr, col: c1, rotation: 0 };
  }
  throw new Error('非相邻格');
}

/**
 * 对羊格集合做多米诺密铺（回溯）
 * @returns {{ row: number, col: number, rotation: number }[] | null}
 */
function tileSheepDominoes(sheepKeysSet) {
  const unplaced = new Set(sheepKeysSet);
  const out = [];

  function neighbors(r, c) {
    return [
      [r + 1, c],
      [r - 1, c],
      [r, c + 1],
      [r, c - 1],
    ];
  }

  function dfs() {
    if (unplaced.size === 0) return true;
    let best = null;
    for (const k of unplaced) {
      if (!best || k < best) best = k;
    }
    const [r, c] = parseKey(best);
    const nbrs = neighbors(r, c);
    for (const [r2, c2] of nbrs) {
      const k2 = key(r2, c2);
      if (!unplaced.has(k2)) continue;
      let pl;
      try {
        pl = dominoPlacement(r, c, r2, c2);
      } catch {
        continue;
      }
      unplaced.delete(best);
      unplaced.delete(k2);
      out.push(pl);
      if (dfs()) return true;
      out.pop();
      unplaced.add(best);
      unplaced.add(k2);
    }
    return false;
  }

  if (dfs()) return out;
  return null;
}

function main() {
  const ws = JSON.parse(fs.readFileSync(ROOT, 'utf8'));
  const sheep = ws.elements.find((e) => e.type === 'sheep' || e.name === '羊');
  const chick = ws.elements.find((e) => e.type === '鸡' || e.name === '鸡' || e.type === 'chick');
  if (!sheep || !chick) throw new Error('workspace.json 需含羊、鸡');

  const sheepEl = cloneEl(sheep);
  const chickEl = cloneEl(chick);

  const sheepKeys = [];
  const chickPl = [];

  for (const group of ROWS) {
    for (const [r, c, t] of group) {
      const R = r + ROW_OFF;
      const C = c + COL_OFF;
      if (t === 's') sheepKeys.push(key(R, C));
      else chickPl.push({ row: R, col: C, rotation: 0, elementId: chickEl.id });
    }
  }

  const tiling = tileSheepDominoes(sheepKeys);
  if (!tiling) {
    throw new Error('羊格无法用两格骨牌完全覆盖（图论无解），请检查参考数据');
  }

  const placements = [];
  let nid = 0;

  tiling.forEach((p) => {
    placements.push({
      id: nid++,
      elementId: sheepEl.id,
      row: p.row,
      col: p.col,
      rotation: p.rotation,
      direction: effectivePlacementDirection(sheepEl, p.rotation),
      param: '',
    });
  });

  chickPl.forEach((p) => {
    placements.push({
      id: nid++,
      elementId: p.elementId,
      row: p.row,
      col: p.col,
      rotation: 0,
      direction: effectivePlacementDirection(chickEl, 0),
      param: '',
    });
  });

  const elementsOut = [sheepEl, chickEl];
  const name = '农场参考图（逐格一致·10×14 嵌入 14×20）';

  const mapJson = {
    version: VERSION,
    name,
    width: MAP_W,
    height: MAP_H,
    elements: elementsOut,
    placements: placements.map((p) => {
      const el = elementsOut.find((e) => e.id === p.elementId);
      return {
        elementId: p.elementId,
        row: p.row,
        col: p.col,
        id: p.id,
        rotation: p.rotation,
        direction: el ? effectivePlacementDirection(el, p.rotation) : 'down',
        param: '',
      };
    }),
  };

  const mapPath = path.join(__dirname, 'level14x20-puzzle.map.json');
  fs.writeFileSync(mapPath, JSON.stringify(mapJson, null, 2), 'utf8');

  const workspaceOut = {
    version: VERSION,
    kind: 'map-editor-workspace',
    elements: elementsOut,
    maps: [
      {
        id: 'level14x20-auto-001',
        name,
        width: MAP_W,
        height: MAP_H,
        elements: elementsOut,
        placements: mapJson.placements.map((p) => ({
          id: p.id,
          elementId: p.elementId,
          row: p.row,
          col: p.col,
          rotation: p.rotation,
          direction: p.direction,
          param: p.param || '',
        })),
        updatedAt: Date.now(),
      },
    ],
  };

  const wsPath = path.join(__dirname, 'workspace.json');
  fs.writeFileSync(wsPath, JSON.stringify(workspaceOut, null, 2), 'utf8');

  console.log('OK', mapPath, 'sheep dominoes:', tiling.length, 'chicks:', chickPl.length, 'total inst:', placements.length);
}

main();
