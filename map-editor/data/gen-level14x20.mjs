/**
 * 按「参考图」风格生成 14×20：椭圆场内约 70–80% 可放格、边角留白、以横向羊为主、纵向羊与鸡填空。
 * 元素来自 map-editor/workspace.json。运行：node gen-level14x20.mjs
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.join(__dirname, '..', 'workspace.json');

const DIR_ORDER = ['down', 'right', 'up', 'left'];
const MAP_W = 14;
const MAP_H = 20;
const VERSION = 4;

/** 固定种子，同一脚本多次运行结果一致 */
const SEED = 20260414;

function mulberry32(a) {
  return function () {
    let t = (a += 0x6d2b79f5);
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function anchorForElement(el) {
  if (!el || !Array.isArray(el.cells) || !el.cells.length) return { r: 0, c: 0 };
  if (el.anchor && Number.isInteger(el.anchor.r) && Number.isInteger(el.anchor.c)) {
    const ok = el.cells.some((c) => c.r === el.anchor.r && c.c === el.anchor.c);
    if (ok) return { r: el.anchor.r, c: el.anchor.c };
  }
  return { r: el.cells[0].r, c: el.cells[0].c };
}

function rotateOffset90CW(dr, dc) {
  return { dr: -dc, dc: dr };
}

function getRotatedCellsAndAnchorRel(baseCells, baseAnchor, rotation) {
  const k = ((rotation % 4) + 4) % 4;
  const rel = baseCells.map(({ r, c }) => ({ dr: r - baseAnchor.r, dc: c - baseAnchor.c }));
  const relRot = rel.map(({ dr, dc }) => {
    let a = { dr, dc };
    for (let i = 0; i < k; i++) a = rotateOffset90CW(a.dr, a.dc);
    return a;
  });
  let minR = Infinity;
  let minC = Infinity;
  relRot.forEach(({ dr, dc }) => {
    minR = Math.min(minR, dr);
    minC = Math.min(minC, dc);
  });
  const cells = relRot.map(({ dr, dc }) => ({ r: dr - minR, c: dc - minC }));
  const anchorRel = { r: -minR, c: -minC };
  return { cells, anchorRel };
}

function getCellsWithRotationForElement(el, rotation) {
  const a = anchorForElement(el);
  return getRotatedCellsAndAnchorRel(el.cells || [], a, rotation).cells;
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

function footprintAtTopLeft(el, topRow, topCol, rotation) {
  const rc = getCellsWithRotationForElement(el, rotation || 0);
  return rc.map(({ r: dr, c: dc }) => ({ row: topRow + dr, col: topCol + dc }));
}

function canPlace(occupied, cells) {
  for (const { row, col } of cells) {
    if (row < 0 || col < 0 || row >= MAP_H || col >= MAP_W) return false;
    if (occupied[row][col]) return false;
  }
  return true;
}

function cloneEl(el) {
  const o = JSON.parse(JSON.stringify(el));
  if (typeof o.hasParam !== 'boolean') o.hasParam = false;
  return o;
}

/** 横向羊优先 rot 1、3；纵向 0、2（元素默认可竖条） */
function rotOrderFor(r, c, rng) {
  const upper = r < MAP_H * 0.48;
  const left = c < MAP_W * 0.38;
  const right = c >= MAP_W * 0.58;
  const midBand = r >= MAP_H * 0.35 && r <= MAP_H * 0.65;

  let horizFirst = [1, 3, 0, 2];
  if (upper && left) horizFirst = [1, 3, 0, 2];
  else if (right) horizFirst = [1, 3, 2, 0];
  else if (midBand && rng() < 0.35) horizFirst = [0, 2, 1, 3];
  else if (rng() < 0.12) horizFirst = [0, 2, 1, 3];

  if (rng() < 0.08) {
    for (let i = horizFirst.length - 1; i > 0; i--) {
      const j = (rng() * (i + 1)) | 0;
      [horizFirst[i], horizFirst[j]] = [horizFirst[j], horizFirst[i]];
    }
  }
  return horizFirst;
}

function main() {
  const rng = mulberry32(SEED);
  const ws = JSON.parse(fs.readFileSync(ROOT, 'utf8'));
  const sheep = ws.elements.find((e) => e.type === 'sheep' || e.name === '羊');
  const chick = ws.elements.find((e) => e.type === '鸡' || e.name === '鸡' || e.type === 'chick');
  if (!sheep || !chick) throw new Error('workspace.json 中需包含羊(sheep)、鸡元素');

  const sheepEl = cloneEl(sheep);
  const chickEl = cloneEl(chick);

  /** 椭圆场内 + 噪声：约 70–80% 总格子可摆放，四角自然空 */
  const fillable = Array.from({ length: MAP_H }, () => Array(MAP_W).fill(false));
  const cx = (MAP_W - 1) / 2;
  const cy = (MAP_H - 1) / 2;
  const rx = MAP_W * 0.46;
  const ry = MAP_H * 0.46;
  let fillCount = 0;
  for (let r = 0; r < MAP_H; r++) {
    for (let c = 0; c < MAP_W; c++) {
      const nx = (c - cx) / rx;
      const ny = (r - cy) / ry;
      if (nx * nx + ny * ny > 1) continue;
      const wave = (Math.sin(r * 0.55) + Math.cos(c * 0.48)) * 0.5 + 0.5;
      const p = 0.72 + wave * 0.14 + (rng() - 0.5) * 0.08;
      if (rng() < p) {
        fillable[r][c] = true;
        fillCount++;
      }
    }
  }

  const occupied = Array.from({ length: MAP_H }, () => Array(MAP_W).fill(false));
  const placements = [];
  let nid = 0;

  function addPlacement(el, row, col, rotation) {
    placements.push({
      id: nid++,
      elementId: el.id,
      row,
      col,
      rotation,
      direction: effectivePlacementDirection(el, rotation),
      param: '',
    });
  }

  function canPlaceSheep(cells) {
    if (!canPlace(occupied, cells)) return false;
    for (const { row, col } of cells) {
      if (!fillable[row][col]) return false;
    }
    return true;
  }

  function tryPlaceSheepCovering(r, c) {
    const order = rotOrderFor(r, c, rng);
    for (const rot of order) {
      const rel = getCellsWithRotationForElement(sheepEl, rot);
      for (const { r: dr, c: dc } of rel) {
        const tr = r - dr;
        const tc = c - dc;
        if (tr < 0 || tc < 0) continue;
        const cells = footprintAtTopLeft(sheepEl, tr, tc, rot);
        if (canPlaceSheep(cells)) {
          cells.forEach(({ row, col }) => {
            occupied[row][col] = true;
          });
          addPlacement(sheepEl, tr, tc, rot);
          return true;
        }
      }
    }
    return false;
  }

  for (let r = 0; r < MAP_H; r++) {
    for (let c = 0; c < MAP_W; c++) {
      if (occupied[r][c]) continue;
      if (!fillable[r][c]) continue;
      if (tryPlaceSheepCovering(r, c)) continue;
      addPlacement(chickEl, r, c, 0);
      occupied[r][c] = true;
    }
  }

  const elementsOut = [sheepEl, chickEl];
  const name = '14×20 农场参考风（椭圆场·横羊为主）';

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

  const sheepN = placements.filter((p) => p.elementId === sheepEl.id).length;
  const chickN = placements.filter((p) => p.elementId === chickEl.id).length;
  let occ = 0;
  for (let r = 0; r < MAP_H; r++) for (let c = 0; c < MAP_W; c++) if (occupied[r][c]) occ++;
  console.log('Wrote', mapPath);
  console.log('Updated', wsPath);
  console.log('fillable~', fillCount, '/', MAP_W * MAP_H, 'instances:', placements.length, 'sheep:', sheepN, 'chick:', chickN, 'cells:', occ, '/', MAP_W * MAP_H);
}

main();
