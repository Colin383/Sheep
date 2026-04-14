/**
 * 一次性脚本：根据关卡坐标生成 map.json（运行：node gen-farm-level.mjs）
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// (row, col) 自顶向下、自左向右，与地图编辑器网格 r=0 为顶行一致
const ROWS = [
  // row 0
  [
    [0, 5, 's'],
    [0, 6, 's'],
  ],
  // row 1
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

const VERSION = 4;
const SHEEP_ID = 'sheep-1';
const CHICK_ID = 'chick-1';

const elements = [
  {
    id: SHEEP_ID,
    name: '羊',
    type: 'sheep',
    gridN: 1,
    cells: [{ r: 0, c: 0 }],
    anchor: { r: 0, c: 0 },
    image: '',
    color: '#e8a8c8',
    direction: 'down',
    hasParam: false,
  },
  {
    id: CHICK_ID,
    name: '鸡',
    type: 'chick',
    gridN: 1,
    cells: [{ r: 0, c: 0 }],
    anchor: { r: 0, c: 0 },
    image: '',
    color: '#ffd54f',
    direction: 'down',
    hasParam: false,
  },
];

let maxR = 0;
let maxC = 0;
const flat = [];
for (const group of ROWS) {
  for (const [r, c, t] of group) {
    flat.push({ r, c, t });
    maxR = Math.max(maxR, r);
    maxC = Math.max(maxC, c);
  }
}

const mapW = maxC + 1;
const mapH = maxR + 1;

const placements = flat.map((x, i) => ({
  id: i,
  elementId: x.t === 'c' ? CHICK_ID : SHEEP_ID,
  row: x.r,
  col: x.c,
  rotation: 0,
  param: '',
  direction: 'down',
}));

const mapJson = {
  version: VERSION,
  name: '农场关卡（参考图）',
  width: mapW,
  height: mapH,
  elements,
  placements: placements.map((p) => {
    const el = elements.find((e) => e.id === p.elementId);
    return {
      elementId: p.elementId,
      row: p.row,
      col: p.col,
      id: p.id,
      rotation: p.rotation,
      direction: el ? el.direction : 'down',
      param: '',
    };
  }),
};

const outPath = path.join(__dirname, 'farm-puzzle-level.map.json');
fs.writeFileSync(outPath, JSON.stringify(mapJson, null, 2), 'utf8');
console.log('Wrote', outPath, `(${placements.length} placements, ${mapW}×${mapH})`);
