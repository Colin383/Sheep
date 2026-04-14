(function () {
  'use strict';

  const VERSION = 4;
  /** 精简游戏导出（与编辑器 .map.json 无关的独立格式） */
  const GAME_EXPORT_VERSION = 1;
  const GAME_EXPORT_KIND = 'map-editor-game';
  const LOCAL_STORAGE_KEY = 'map-editor-elements-v2';
  const LOCAL_STORAGE_MAPS_KEY = 'map-editor-saved-maps-v1';
  /** 地图页：锚点 / 实例 ID / 实例参数 等工具栏复选框 */
  const LOCAL_STORAGE_MAP_VIEW_PREFS_KEY = 'map-editor-map-view-prefs-v1';
  const WORKSPACE_KIND = 'map-editor-workspace';
  const WORKSPACE_JSON_PATH = 'data/workspace.json';
  /** IndexedDB：记住「导出工作台」写入的本地文件句柄，便于下次覆盖 */
  const IDB_FS_NAME = 'map-editor-fs';
  const IDB_FS_STORE = 'handles';
  const IDB_WORKSPACE_EXPORT_KEY = 'workspaceExport';
  /** IndexedDB：记住「批量导出地图」所选文件夹句柄，便于下次直接写入 */
  const IDB_BATCH_EXPORT_DIR_KEY = 'batchExportDir';
  const CELL_EL = 28;
  const CELL_MAP = 32;
  const GAP = 2;
  const STEP_MAP = CELL_MAP + GAP;
  const MAP_AXIS_TOP_PX = 22;
  const MAP_AXIS_LEFT_PX = 36;

  /** @type {{ id: string, name: string, type: string, gridN: number, cells: {r:number,c:number}[], anchor?: {r:number,c:number}, image: string, color: string, direction?: string, hasParam?: boolean }}[] */
  let elements = [];

  let elGridN = 4;
  /** @type {boolean[][]} */
  let elSelection = [];
  /** @type {{r:number,c:number} | null} */
  let elAnchorCell = null;

  let elImageDataUrl = '';

  /** 弹窗编辑中的元素 id，关闭弹窗后为 null */
  let modalEditElementId = null;
  let modalElGridN = 4;
  /** @type {boolean[][]} */
  let modalElSelection = [];
  /** @type {{r:number,c:number} | null} */
  let modalElAnchorCell = null;
  let modalElImageDataUrl = '';

  let mapW = 14;
  let mapH = 20;
  /** @type {{ id: number | string, elementId: string, row: number, col: number, rotation?: number, param?: string }[]} */
  let placements = [];

  /** 下一个地图实例 id（从 0 递增，均为非负整数） */
  let nextPlacementId = 0;

  let selectedElementId = null;
  /** 放置前旋转（0–3，每次按 R 顺时针 90°） */
  let pendingRotation = 0;
  /** 放置预览时保留字段：当前版本固定按元素锚点计算 top-left */
  let placementPivotRel = null;
  /** 移动模式下选中的实例 */
  let selectedPlacementId = null;
  let mapHoverRow = -1;
  let mapHoverCol = -1;

  /** @type {{ id: string, name: string, width: number, height: number, elements: object[], placements: object[], updatedAt: number }[]} */
  let savedMaps = [];
  /** @type {string | null} 当前正在编辑的列表项 id，未关联列表则为 null */
  let activeMapId = null;

  /** 与「已保存到列表」或加载后的状态比较，用于未保存提示 */
  let mapEditBaseline = null;

  const MAX_MAP_UNDO = 50;
  /** @type {string[]} */
  let mapUndoStack = [];
  /** @type {string[]} */
  let mapRedoStack = [];
  let mapHistoryRestoring = false;

  /** @type {string | null} */
  let pendingMapLoadId = null;
  /** @type {object | null} */
  let pendingImportPayload = null;
  let showMapAnchors = true;
  /** 左上角 #id 徽标 */
  let showPlacementIds = true;
  /** 带参数元素的参数文本（半透明底） */
  let showPlacementParams = true;

  // --- DOM ---
  const $ = (id) => document.getElementById(id);

  const tabButtons = document.querySelectorAll('.tab');
  const panelElements = $('panel-elements');
  const panelMap = $('panel-map');

  const elGridNInput = $('el-grid-n');
  const btnCreateElCanvas = $('btn-create-el-canvas');
  const elementCanvas = $('element-canvas');
  const elName = $('el-name');
  const elType = $('el-type');
  const elImageInput = $('el-image');
  const elColorInput = $('el-color');
  const elDirectionInput = $('el-direction');
  const elHasParamInput = $('el-has-param');
  const elImagePreview = $('el-image-preview');
  const btnSaveElement = $('btn-save-element');
  const btnClearSelection = $('btn-clear-selection');

  const elementEditDialog = $('element-edit-dialog');
  const btnElementEditClose = $('element-edit-close');
  const elModalGridNInput = $('el-modal-grid-n');
  const btnModalCreateElCanvas = $('btn-modal-create-el-canvas');
  const elementModalCanvas = $('element-modal-canvas');
  const elModalName = $('el-modal-name');
  const elModalType = $('el-modal-type');
  const elModalDirection = $('el-modal-direction');
  const elModalHasParamInput = $('el-modal-has-param');
  const elModalImageInput = $('el-modal-image');
  const elModalColorInput = $('el-modal-color');
  const elModalImagePreview = $('el-modal-image-preview');
  const elModalStatus = $('el-modal-status');
  const btnModalSaveElement = $('btn-modal-save-element');
  const btnModalClearSelection = $('btn-modal-clear-selection');
  const elStatus = $('el-status');
  const elementList = $('element-list');
  const btnExportElements = $('btn-export-elements');
  const importElements = $('import-elements');

  const mapName = $('map-name');
  const mapGridWInput = $('map-grid-w');
  const mapGridHInput = $('map-grid-h');
  const btnCreateMap = $('btn-create-map');
  const mapCanvas = $('map-canvas');
  const mapCanvasWrap = $('map-canvas-wrap');
  const mapHoverLayer = $('map-hover-layer');
  const mapHoverFloatLabel = $('map-hover-float-label');
  const mapPlacementLabels = $('map-placement-labels');
  const mapStatus = $('map-status');
  const mapShowAnchorToggle = $('map-show-anchor-toggle');
  const mapShowPlacementIdToggle = $('map-show-placement-id-toggle');
  const mapShowPlacementParamToggle = $('map-show-placement-param-toggle');
  const mapElementList = $('map-element-list');
  const btnSaveMap = $('btn-save-map');
  const btnExportGameJson = $('btn-export-game-json');
  const importMap = $('import-map');
  const btnExportWorkspace = $('btn-export-workspace');
  const importWorkspace = $('import-workspace');
  const btnSaveMapLibrary = $('btn-save-map-library');
  const btnSaveMapAsNew = $('btn-save-map-as-new');
  const btnMapUndo = $('btn-map-undo');
  const btnMapRedo = $('btn-map-redo');
  const mapList = $('map-list');
  const btnOpenBatchExport = $('btn-open-batch-export');
  const batchExportDialog = $('batch-export-dialog');
  const batchExportMapList = $('batch-export-map-list');
  const batchExportClose = $('batch-export-close');
  const batchExportSelectAll = $('batch-export-select-all');
  const batchExportSelectNone = $('batch-export-select-none');
  const batchExportConfirm = $('batch-export-confirm');
  const batchExportCancel = $('batch-export-cancel');

  const mapUnsavedDialog = $('map-unsaved-dialog');
  const mapUnsavedSave = $('map-unsaved-save');
  const mapUnsavedDiscard = $('map-unsaved-discard');
  const mapUnsavedCancel = $('map-unsaved-cancel');

  const placementParamDialog = $('placement-param-dialog');
  const placementParamHint = $('placement-param-hint');
  const placementParamInput = $('placement-param-input');
  const placementParamOk = $('placement-param-ok');
  const placementParamCancel = $('placement-param-cancel');

  const toastEl = $('toast');

  /** @type {string | number | null} */
  let placementParamEditPid = null;

  /** 批量导出：地图 id 顺序（与列表拖拽一致） */
  let batchExportOrderIds = [];
  /** @type {Record<string, boolean>} */
  let batchExportChecked = {};
  let batchExportDragId = null;

  /** @type {FileSystemFileHandle | null} */
  let workspaceExportFileHandle = null;
  /** @type {FileSystemDirectoryHandle | null} */
  let batchExportDirHandle = null;

  // --- Map axis labels (row/col numbers) ---
  let mapAxisCorner = null;
  let mapAxisTop = null;
  let mapAxisLeft = null;

  function ensureMapAxisLayers() {
    if (!mapCanvasWrap) return;
    mapCanvasWrap.style.setProperty('--map-axis-top', MAP_AXIS_TOP_PX + 'px');
    mapCanvasWrap.style.setProperty('--map-axis-left', MAP_AXIS_LEFT_PX + 'px');

    if (!mapAxisCorner) {
      const el = document.createElement('div');
      el.id = 'map-axis-corner';
      el.className = 'map-axis map-axis-corner';
      el.setAttribute('aria-hidden', 'true');
      el.textContent = '行/列';
      mapAxisCorner = el;
      mapCanvasWrap.insertBefore(el, mapCanvasWrap.firstChild);
    }

    if (!mapAxisTop) {
      const el = document.createElement('div');
      el.id = 'map-axis-top';
      el.className = 'map-axis map-axis-top';
      el.setAttribute('aria-hidden', 'true');
      mapAxisTop = el;
      mapCanvasWrap.insertBefore(el, mapAxisCorner.nextSibling);
    }

    if (!mapAxisLeft) {
      const el = document.createElement('div');
      el.id = 'map-axis-left';
      el.className = 'map-axis map-axis-left';
      el.setAttribute('aria-hidden', 'true');
      mapAxisLeft = el;
      mapCanvasWrap.insertBefore(el, mapAxisTop.nextSibling);
    }
  }

  function renderMapAxis() {
    if (!mapCanvasWrap) return;
    ensureMapAxisLayers();
    if (!mapAxisTop || !mapAxisLeft || !mapAxisCorner) return;

    const innerW = mapW * STEP_MAP - GAP;
    const innerH = mapH * STEP_MAP - GAP;

    mapAxisTop.style.width = innerW + 'px';
    mapAxisLeft.style.height = innerH + 'px';

    mapAxisTop.style.gridTemplateColumns = `repeat(${mapW}, ${CELL_MAP}px)`;
    mapAxisLeft.style.gridTemplateRows = `repeat(${mapH}, ${CELL_MAP}px)`;

    mapAxisTop.innerHTML = '';
    mapAxisLeft.innerHTML = '';

    const topFrag = document.createDocumentFragment();
    for (let c = 0; c < mapW; c++) {
      const t = document.createElement('div');
      t.className = 'map-axis-tick';
      t.textContent = String(c);
      topFrag.appendChild(t);
    }
    mapAxisTop.appendChild(topFrag);

    const leftFrag = document.createDocumentFragment();
    for (let r = 0; r < mapH; r++) {
      const t = document.createElement('div');
      t.className = 'map-axis-tick';
      // 显示坐标系：左下角为 (0,0)，因此纵轴需要反向显示
      t.textContent = String(mapH - 1 - r);
      leftFrag.appendChild(t);
    }
    mapAxisLeft.appendChild(leftFrag);
  }

  function uid() {
    return crypto.randomUUID ? crypto.randomUUID() : 'id-' + Date.now() + '-' + Math.random().toString(36).slice(2);
  }

  function isPlainNumericId(id) {
    if (typeof id === 'number' && Number.isInteger(id) && id >= 0) return true;
    if (typeof id === 'string' && /^\d+$/.test(id)) return true;
    return false;
  }

  function syncNextPlacementIdFromPlacements() {
    let max = -1;
    placements.forEach((p) => {
      if (typeof p.id === 'number' && Number.isInteger(p.id) && p.id >= 0) max = Math.max(max, p.id);
      else if (typeof p.id === 'string' && /^\d+$/.test(p.id)) max = Math.max(max, parseInt(p.id, 10));
    });
    nextPlacementId = max + 1;
  }

  /** 从文件/列表加载地图时：非数字 id（如旧版 UUID）则按顺序改为 0…n-1 */
  function migratePlacementIdsIfNeeded() {
    if (placements.some((p) => !isPlainNumericId(p.id))) {
      placements.forEach((p, i) => {
        p.id = i;
      });
      nextPlacementId = placements.length;
      return;
    }
    placements.forEach((p) => {
      if (typeof p.id === 'string' && /^\d+$/.test(p.id)) p.id = parseInt(p.id, 10);
    });
    syncNextPlacementIdFromPlacements();
  }

  /** 导出/保存前：保证显示的 id 与导出的 id 同步 */
  function normalizePlacementIdsBeforeExport() {
    const before = placements.map((p) => String(p.id)).join('|');
    migratePlacementIdsIfNeeded();
    const after = placements.map((p) => String(p.id)).join('|');
    if (before !== after) {
      // id 有变化则重绘，确保 UI 徽标与导出一致
      renderMap();
      renderMapElementList();
      renderMapList();
      showToast('已规范化实例 id（导出与显示保持一致）', 'ok');
    }
  }

  function allocatePlacementId() {
    return nextPlacementId++;
  }

  function hexToRgb(hex) {
    const m = /^#?([0-9a-fA-F]{6})$/.exec(String(hex || '').trim());
    if (!m) return { r: 68, g: 136, b: 204 };
    const n = parseInt(m[1], 16);
    return { r: (n >> 16) & 255, g: (n >> 8) & 255, b: n & 255 };
  }

  function hexToRgba(hex, a) {
    const { r, g, b } = hexToRgb(hex);
    return 'rgba(' + r + ',' + g + ',' + b + ',' + a + ')';
  }

  function darkenHex(hex, factor) {
    const { r, g, b } = hexToRgb(hex);
    const f = Math.max(0, Math.min(1, factor));
    return (
      '#' +
      [r, g, b]
        .map((x) =>
          Math.round(x * f)
            .toString(16)
            .padStart(2, '0')
        )
        .join('')
    );
  }

  function getElementColor(el) {
    if (el && el.color && /^#[0-9a-fA-F]{6}$/.test(el.color)) return el.color;
    return '#4488cc';
  }

  const DIR_ORDER = ['down', 'right', 'up', 'left'];
  const ARROW_CHARS = ['↓', '→', '↑', '←'];

  function normalizeElement(x) {
    const d = x.direction;
    const direction = DIR_ORDER.indexOf(d) >= 0 ? d : 'down';
    const cells = Array.isArray(x.cells) ? x.cells.map((c) => ({ r: c.r, c: c.c })) : [];
    const fallbackAnchor = cells.length ? { r: cells[0].r, c: cells[0].c } : { r: 0, c: 0 };
    const rawAnchor = x.anchor && typeof x.anchor === 'object' ? x.anchor : fallbackAnchor;
    let anchor = { r: Number(rawAnchor.r), c: Number(rawAnchor.c) };
    if (!Number.isInteger(anchor.r) || !Number.isInteger(anchor.c)) anchor = fallbackAnchor;
    const hasAnchorCell = cells.some((c) => c.r === anchor.r && c.c === anchor.c);
    if (!hasAnchorCell) anchor = fallbackAnchor;
    return {
      ...x,
      cells,
      anchor,
      color: x.color && /^#[0-9a-fA-F]{6}$/.test(x.color) ? x.color : '#4488cc',
      image: x.image || '',
      direction: direction,
      hasParam: Boolean(x.hasParam),
    };
  }

  /** 从文件/列表加载地图时统一实例字段 */
  function normalizePlacementRow(p) {
    const rot = typeof p.rotation === 'number' ? ((p.rotation % 4) + 4) % 4 : 0;
    return {
      id: p.id,
      elementId: p.elementId,
      row: p.row,
      col: p.col,
      rotation: rot,
      param: typeof p.param === 'string' ? p.param : '',
    };
  }

  function getDirection(el) {
    const d = el && el.direction;
    return DIR_ORDER.indexOf(d) >= 0 ? d : 'down';
  }

  function anchorForElement(el) {
    if (!el || !Array.isArray(el.cells) || !el.cells.length) return { r: 0, c: 0 };
    if (el.anchor && Number.isInteger(el.anchor.r) && Number.isInteger(el.anchor.c)) {
      const ok = el.cells.some((c) => c.r === el.anchor.r && c.c === el.anchor.c);
      if (ok) return { r: el.anchor.r, c: el.anchor.c };
    }
    return { r: el.cells[0].r, c: el.cells[0].c };
  }

  function directionIndex(d) {
    const i = DIR_ORDER.indexOf(d);
    return i >= 0 ? i : 0;
  }

  /** 元素基础朝向 + 地图上旋转 → 最终朝向（与箭头一致），用于导出 JSON */
  function effectivePlacementDirection(el, rotation) {
    const ri = (directionIndex(getDirection(el)) + ((rotation || 0) % 4) + 4) % 4;
    return DIR_ORDER[ri];
  }

  function effectiveArrowChar(el, placementRotation) {
    const ri = (directionIndex(getDirection(el)) + ((placementRotation || 0) % 4) + 4) % 4;
    return ARROW_CHARS[ri];
  }

  function normalizeCellsCoords(cells) {
    if (!cells.length) return [];
    let minR = Infinity,
      minC = Infinity;
    cells.forEach(({ r, c }) => {
      minR = Math.min(minR, r);
      minC = Math.min(minC, c);
    });
    return cells.map(({ r, c }) => ({ r: r - minR, c: c - minC }));
  }

  function rotateCells90CW(cells) {
    if (!cells.length) return [];
    let maxR = 0;
    cells.forEach(({ r }) => {
      maxR = Math.max(maxR, r);
    });
    return cells.map(({ r, c }) => ({ r: c, c: maxR - r }));
  }

  function bboxExtents(cells) {
    if (!cells.length) return { minR: 0, maxR: 0, minC: 0, maxC: 0 };
    let minR = Infinity,
      minC = Infinity,
      maxR = -Infinity,
      maxC = -Infinity;
    cells.forEach(({ r, c }) => {
      minR = Math.min(minR, r);
      minC = Math.min(minC, c);
      maxR = Math.max(maxR, r);
      maxC = Math.max(maxC, c);
    });
    return { minR, maxR, minC, maxC };
  }

  /** 绕外接矩形几何中心 (pr,pc) 顺时针 90°（pr/pc 可为半格），再归一化并取整格 */
  function rotateCells90CWAboutPivot(cells, pr, pc) {
    if (!cells.length) return [];
    const shifted = cells.map(({ r, c }) => ({ r: r - pr, c: c - pc }));
    let maxR = -Infinity;
    shifted.forEach(({ r }) => {
      maxR = Math.max(maxR, r);
    });
    const rotated = shifted.map(({ r, c }) => ({ r: c, c: maxR - r }));
    const back = rotated.map(({ r, c }) => ({
      r: Math.round(r + pr),
      c: Math.round(c + pc),
    }));
    const seen = new Set();
    const deduped = [];
    back.forEach(({ r, c }) => {
      const k = `${r},${c}`;
      if (!seen.has(k)) {
        seen.add(k);
        deduped.push({ r, c });
      }
    });
    return normalizeCellsCoords(deduped);
  }

  /** 绕当前外接矩形几何中心顺时针旋转 k×90°（1×2 等偶数边绕两格之间中心，不会第三次像翻转） */
  function getNormalizedRotatedCells(baseCells, rotation) {
    let cells = baseCells.map((x) => ({ r: x.r, c: x.c }));
    const k = ((rotation % 4) + 4) % 4;
    for (let i = 0; i < k; i++) {
      const { minR, maxR, minC, maxC } = bboxExtents(cells);
      const pr = (minR + maxR) / 2;
      const pc = (minC + maxC) / 2;
      cells = rotateCells90CWAboutPivot(cells, pr, pc);
    }
    return cells;
  }

  /** 鼠标所在格对齐「中心格」时地图锚点（与 floor((h-1)/2) 语义一致） */
  function anchorFromHover(hoverRow, hoverCol, normalizedCells) {
    const bb = bboxOfCells(normalizedCells);
    const pr = Math.floor((bb.h - 1) / 2);
    const pc = Math.floor((bb.w - 1) / 2);
    return { row: hoverRow - pr, col: hoverCol - pc };
  }

  /** 地图绝对格坐标绕整数枢轴格顺时针 90°（行向下、列向右）：(dr,dc) 相对枢轴 -> (-dc, dr) */
  function rotateMapCells90CWAboutPivot(absCells, pivotR, pivotC) {
    const out = [];
    const seen = new Set();
    absCells.forEach(({ r, c }) => {
      const dr = r - pivotR;
      const dc = c - pivotC;
      const dr2 = -dc;
      const dc2 = dr;
      const nr = pivotR + dr2;
      const nc = pivotC + dc2;
      const k = `${nr},${nc}`;
      if (!seen.has(k)) {
        seen.add(k);
        out.push({ r: nr, c: nc });
      }
    });
    return out;
  }

  function cellsEqualAsSet(a, b) {
    if (a.length !== b.length) return false;
    const setB = new Set(b.map(({ r, c }) => `${r},${c}`));
    for (let i = 0; i < a.length; i++) {
      const key = `${a[i].r},${a[i].c}`;
      if (!setB.has(key)) return false;
    }
    return true;
  }

  /** 与 getNormalizedRotatedCells(base, k) 某一 k 一致时返回该 k，否则 -1 */
  function rotationIndexForNormalizedShape(baseCells, normalizedTarget) {
    for (let k = 0; k < 4; k++) {
      const cand = getNormalizedRotatedCells(baseCells, k);
      if (cellsEqualAsSet(cand, normalizedTarget)) return k;
    }
    return -1;
  }

  /**
   * 绕枢轴顺时针 90° 后反推 rotation：优先 (oldRot+1)%4，避免 1×2 等对称形在 0 与 2 上歧义导致箭头仍显示为 0° 状态。
   */
  function resolveRotationAfterPivotStep(baseCells, oldRot, newRel) {
    const prev = ((oldRot % 4) + 4) % 4;
    const stepped = (prev + 1) % 4;
    const cand = getNormalizedRotatedCells(baseCells, stepped);
    if (cellsEqualAsSet(cand, newRel)) return stepped;
    return rotationIndexForNormalizedShape(baseCells, newRel);
  }

  function nearestFootprintCellAbs(absCells, tr, tc) {
    let best = absCells[0];
    let bestD = Infinity;
    absCells.forEach(({ r, c }) => {
      const d = (r - tr) * (r - tr) + (c - tc) * (c - tc);
      if (d < bestD) {
        bestD = d;
        best = { r, c };
      }
    });
    return best;
  }

  function getCellsWithRotation(baseCells, rotation) {
    return getNormalizedRotatedCells(baseCells, rotation);
  }

  function rotateOffset90CW(dr, dc) {
    return { dr: -dc, dc: dr };
  }

  /**
   * 以元素锚点格旋转后的 footprint（归一化）与锚点在归一化后的相对位置。
   * @param {{r:number,c:number}[]} baseCells
   * @param {{r:number,c:number}} baseAnchor
   * @param {number} rotation
   */
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

  function topLeftFromAnchorCell(el, anchorRow, anchorCol, rotation) {
    const a = anchorForElement(el);
    const d = getRotatedCellsAndAnchorRel(el.cells || [], a, rotation);
    return { row: anchorRow - d.anchorRel.r, col: anchorCol - d.anchorRel.c };
  }

  function anchorCellFromTopLeft(el, row, col, rotation) {
    const a = anchorForElement(el);
    const d = getRotatedCellsAndAnchorRel(el.cells || [], a, rotation);
    return { row: row + d.anchorRel.r, col: col + d.anchorRel.c };
  }

  function isColorOnlyElement(el) {
    return !el.image || String(el.image).trim() === '';
  }

  function showToast(msg, kind) {
    toastEl.textContent = msg;
    toastEl.className = 'toast show' + (kind === 'err' ? ' err' : kind === 'ok' ? ' ok' : '');
    clearTimeout(showToast._t);
    showToast._t = setTimeout(() => {
      toastEl.classList.remove('show');
    }, 2600);
  }

  function normalizeCells(selected, gridN) {
    const coords = [];
    for (let r = 0; r < gridN; r++) {
      for (let c = 0; c < gridN; c++) {
        if (selected[r] && selected[r][c]) coords.push({ r, c });
      }
    }
    if (coords.length === 0) return [];
    let minR = Infinity,
      minC = Infinity;
    coords.forEach(({ r, c }) => {
      minR = Math.min(minR, r);
      minC = Math.min(minC, c);
    });
    return coords.map(({ r, c }) => ({ r: r - minR, c: c - minC }));
  }

  function normalizeAnchorFromSelection(selected, gridN, anchor) {
    if (!anchor) return null;
    const coords = [];
    for (let r = 0; r < gridN; r++) {
      for (let c = 0; c < gridN; c++) {
        if (selected[r] && selected[r][c]) coords.push({ r, c });
      }
    }
    if (!coords.length) return null;
    let minR = Infinity;
    let minC = Infinity;
    coords.forEach(({ r, c }) => {
      minR = Math.min(minR, r);
      minC = Math.min(minC, c);
    });
    return { r: anchor.r - minR, c: anchor.c - minC };
  }

  function bboxOfCells(cells) {
    if (!cells.length) return { w: 0, h: 0 };
    let maxR = 0,
      maxC = 0;
    cells.forEach(({ r, c }) => {
      maxR = Math.max(maxR, r);
      maxC = Math.max(maxC, c);
    });
    return { w: maxC + 1, h: maxR + 1 };
  }

  /** 将相对格子平移到 min 为 0，并返回 min 偏移（用于地图锚点与整块填充） */
  function normalizeFootprintCells(rc) {
    let minDr = Infinity;
    let minDc = Infinity;
    rc.forEach(({ r, c }) => {
      minDr = Math.min(minDr, r);
      minDc = Math.min(minDc, c);
    });
    if (!Number.isFinite(minDr) || !Number.isFinite(minDc)) return { norm: [], minDr: 0, minDc: 0 };
    const norm = rc.map(({ r, c }) => ({ r: r - minDr, c: c - minDc }));
    return { norm, minDr, minDc };
  }

  /** 是否为完整矩形（无空洞），仅此类可用单层色块盖住格子缝隙 */
  function isFullRectangleFootprintNorm(norm) {
    const bb = bboxOfCells(norm);
    if (bb.w === 0 || bb.h === 0) return false;
    if (norm.length !== bb.w * bb.h) return false;
    const set = new Set(norm.map(({ r, c }) => `${r},${c}`));
    for (let r = 0; r < bb.h; r++) {
      for (let c = 0; c < bb.w; c++) {
        if (!set.has(`${r},${c}`)) return false;
      }
    }
    return true;
  }

  function neighborInPreviewFootprint(keySet, r, c, bh, bw) {
    if (r < 0 || c < 0 || r >= bh || c >= bw) return false;
    return keySet.has(`${r},${c}`);
  }

  function listPreviewCellSize(bw, bh) {
    const maxPx = 96;
    const w = Math.max(1, bw);
    const h = Math.max(1, bh);
    const stepW = (maxPx + GAP) / w;
    const stepH = (maxPx + GAP) / h;
    const step = Math.min(stepW, stepH, 34);
    return Math.max(10, Math.floor(step - GAP));
  }

  /**
   * @param {HTMLElement} gridEl
   * @param {{ cells: {r:number,c:number}[], image?: string, color?: string }} el
   * @param {{ cellSize?: number }} [opt]
   */
  function renderElementFootprintIntoGrid(gridEl, el, opt) {
    opt = opt || {};
    const cellSize = opt.cellSize != null ? opt.cellSize : CELL_MAP;
    const step = cellSize + GAP;
    const normalized = el.cells;
    if (!normalized || !normalized.length) {
      gridEl.innerHTML = '';
      return;
    }
    const bb = bboxOfCells(normalized);
    const bh = bb.h;
    const bw = bb.w;
    const anchor = opt.anchor || null;
    const keySet = new Set();
    normalized.forEach(({ r, c }) => keySet.add(`${r},${c}`));
    gridEl.style.gridTemplateColumns = 'repeat(' + bw + ', ' + cellSize + 'px)';
    gridEl.style.gridTemplateRows = 'repeat(' + bh + ', ' + cellSize + 'px)';
    gridEl.innerHTML = '';
    const bpx = Math.max(1, Math.round(cellSize * 0.12));
    for (let r = 0; r < bh; r++) {
      for (let c = 0; c < bw; c++) {
        const cell = document.createElement('div');
        cell.className = 'cell';
        if (!keySet.has(`${r},${c}`)) {
          cell.classList.add('el-preview-empty');
          gridEl.appendChild(cell);
          continue;
        }
        if (!isColorOnlyElement(el)) {
          cell.classList.add('has-image');
          const tw = bw * step - GAP;
          const th = bh * step - GAP;
          cell.style.backgroundImage = 'url(' + el.image + ')';
          cell.style.backgroundSize = tw + 'px ' + th + 'px';
          cell.style.backgroundPosition = -c * step + 'px ' + -r * step + 'px';
        } else {
          const fillColor = getElementColor(el);
          cell.classList.add('has-color');
          cell.style.backgroundColor = hexToRgba(fillColor, 0.62);
          const edge = darkenHex(fillColor, 0.4);
          if (!neighborInPreviewFootprint(keySet, r - 1, c, bh, bw)) cell.style.borderTop = bpx + 'px solid ' + edge;
          if (!neighborInPreviewFootprint(keySet, r + 1, c, bh, bw)) cell.style.borderBottom = bpx + 'px solid ' + edge;
          if (!neighborInPreviewFootprint(keySet, r, c - 1, bh, bw)) cell.style.borderLeft = bpx + 'px solid ' + edge;
          if (!neighborInPreviewFootprint(keySet, r, c + 1, bh, bw)) cell.style.borderRight = bpx + 'px solid ' + edge;
        }
        if (anchor && anchor.r === r && anchor.c === c) {
          cell.classList.add('cell-anchor');
        }
        gridEl.appendChild(cell);
      }
    }
  }

  function renderListItemPreview(el) {
    const wrap = document.createElement('div');
    wrap.className = 'element-item-preview-wrap';
    const stage = document.createElement('div');
    stage.className = 'el-live-preview-stage element-item-preview-stage';
    const grid = document.createElement('div');
    grid.className = 'grid-canvas map-grid el-preview-grid element-mini-grid';
    const bb = bboxOfCells(el.cells);
    const cellSize = listPreviewCellSize(bb.w, bb.h);
    renderElementFootprintIntoGrid(grid, el, { cellSize: cellSize, anchor: anchorForElement(el) });
    stage.appendChild(grid);
    if (isColorOnlyElement(el)) {
      const label = document.createElement('div');
      label.className = 'placement-label el-preview-label element-mini-label placement-label-stack';
      const arr = document.createElement('span');
      arr.className = 'placement-arrow';
      arr.textContent = ARROW_CHARS[directionIndex(getDirection(el))];
      const nm = document.createElement('span');
      nm.className = 'placement-name';
      nm.textContent = el.name;
      label.appendChild(arr);
      label.appendChild(nm);
      const step = cellSize + GAP;
      const left = (bb.w * step - GAP) / 2;
      const top = (bb.h * step - GAP) / 2;
      label.style.left = left + 'px';
      label.style.top = top + 'px';
      label.style.fontSize = Math.max(8, Math.min(11, Math.round(cellSize * 0.55))) + 'px';
      label.style.maxWidth = Math.ceil(bb.w * step) + 'px';
      stage.appendChild(label);
    }
    wrap.appendChild(stage);
    return wrap;
  }

  function persistElements() {
    try {
      localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify({ version: VERSION, elements: elements }));
    } catch (e) {
      console.warn('persistElements', e);
    }
  }

  /** 与 footprint 匹配的画布边长：优先使用元素记录的 gridN，否则取外接正方形边长 */
  function gridNForElement(el) {
    const bb = bboxOfCells(el.cells || []);
    const need = Math.max(bb.w, bb.h, 1);
    const gn = parseInt(String(el.gridN != null ? el.gridN : ''), 10);
    if (Number.isFinite(gn) && gn >= need && gn <= 64) return gn;
    return Math.min(64, need);
  }

  /** 将更新后的元素定义写入所有已保存地图中的内嵌 elements 副本（与导出 .map.json 结构一致） */
  function syncElementIntoSavedMaps(updatedEl) {
    const copy = JSON.parse(JSON.stringify(normalizeElement(updatedEl)));
    let changed = false;
    savedMaps.forEach((m) => {
      if (!Array.isArray(m.elements)) return;
      const idx = m.elements.findIndex((e) => e.id === copy.id);
      if (idx >= 0) {
        m.elements[idx] = copy;
        changed = true;
      }
    });
    if (changed) persistMapLibrary();
  }

  function closeElementEditModal() {
    if (elementEditDialog) elementEditDialog.hidden = true;
    modalEditElementId = null;
  }

  function isElementEditModalOpen() {
    return elementEditDialog && !elementEditDialog.hidden;
  }

  function isPlacementParamDialogOpen() {
    return placementParamDialog && !placementParamDialog.hidden;
  }

  function closePlacementParamDialog() {
    if (placementParamDialog) placementParamDialog.hidden = true;
    placementParamEditPid = null;
    if (placementParamInput) placementParamInput.value = '';
  }

  function openPlacementParamDialog(pid) {
    const p = placements.find((x) => x.id == pid);
    if (!p) return;
    const el = elements.find((e) => e.id === p.elementId);
    if (!el || !el.hasParam) return;
    placementParamEditPid = pid;
    if (placementParamHint) {
      placementParamHint.textContent = '元素「' + (el.name || '') + '」· 实例 id ' + p.id;
    }
    if (placementParamInput) {
      placementParamInput.value = typeof p.param === 'string' ? p.param : '';
    }
    if (placementParamDialog) placementParamDialog.hidden = false;
    if (placementParamInput) {
      try {
        placementParamInput.focus();
        placementParamInput.select();
      } catch (e) {
        /* ignore */
      }
    }
  }

  function updateModalElStatus() {
    if (!elModalStatus) return;
    const n = normalizeCells(modalElSelection, modalElGridN).length;
    if (!n) {
      elModalStatus.textContent = '请在画布上点选至少一格';
      return;
    }
    const a = modalElAnchorCell ? `，锚点 (${modalElAnchorCell.r},${modalElAnchorCell.c})` : '，请右键一个已选格子设为锚点';
    elModalStatus.textContent = `已选 ${n} 格${a}`;
  }

  function repaintCanvasAnchorMark(canvasEl, anchor) {
    if (!canvasEl) return;
    canvasEl.querySelectorAll('.cell.cell-anchor').forEach((x) => x.classList.remove('cell-anchor'));
    if (!anchor) return;
    const cell = canvasEl.querySelector('.cell[data-r="' + anchor.r + '"][data-c="' + anchor.c + '"]');
    if (cell) cell.classList.add('cell-anchor');
  }

  function refreshModalLiveElementPreview() {
    const placeholder = $('el-modal-live-preview-placeholder');
    const inner = $('el-modal-live-preview-inner');
    const grid = $('el-modal-live-preview-grid');
    const labelEl = $('el-modal-live-preview-label');
    if (!placeholder || !inner || !grid || !labelEl) return;

    const normalized = normalizeCells(modalElSelection, modalElGridN);
    if (!normalized.length) {
      placeholder.hidden = false;
      inner.hidden = true;
      return;
    }

    placeholder.hidden = true;
    inner.hidden = false;

    const bb = bboxOfCells(normalized);
    const bh = bb.h;
    const bw = bb.w;

    const fakeEl = {
      cells: normalized,
      image: modalElImageDataUrl || '',
      color: elModalColorInput ? elModalColorInput.value : '#4488cc',
      name: elModalName && elModalName.value.trim() ? elModalName.value.trim() : '未命名元素',
      direction: elModalDirection && elModalDirection.value ? elModalDirection.value : 'down',
    };

    renderElementFootprintIntoGrid(grid, fakeEl, { cellSize: CELL_MAP });

    if (isColorOnlyElement(fakeEl)) {
      labelEl.hidden = false;
      labelEl.className = 'placement-label el-preview-label placement-label-stack';
      labelEl.innerHTML = '';
      const arr = document.createElement('span');
      arr.className = 'placement-arrow';
      arr.textContent = ARROW_CHARS[directionIndex(getDirection(fakeEl))];
      const nm = document.createElement('span');
      nm.className = 'placement-name';
      nm.textContent = fakeEl.name;
      labelEl.appendChild(arr);
      labelEl.appendChild(nm);
      const left = (bw * STEP_MAP - GAP) / 2;
      const top = (bh * STEP_MAP - GAP) / 2;
      labelEl.style.left = left + 'px';
      labelEl.style.top = top + 'px';
    } else {
      labelEl.hidden = true;
      labelEl.className = 'placement-label el-preview-label';
      labelEl.textContent = '';
    }
  }

  function refreshModalElementCanvasPreview() {
    if (!elementModalCanvas) return;
    const color = elModalColorInput ? elModalColorInput.value : '#4488cc';
    elementModalCanvas.querySelectorAll('.cell').forEach((cell) => {
      const isOn = cell.classList.contains('on');
      if (!modalElImageDataUrl && isOn) {
        cell.style.backgroundColor = hexToRgba(color, 0.55);
        cell.style.boxShadow = 'inset 0 0 0 2px ' + darkenHex(color, 0.5);
      } else if (isOn) {
        cell.style.backgroundColor = '';
        cell.style.boxShadow = '';
      } else {
        cell.style.backgroundColor = '';
        cell.style.boxShadow = '';
      }
    });
    refreshModalLiveElementPreview();
  }

  function buildModalElementCanvas() {
    if (!elModalGridNInput || !elementModalCanvas) return;
    modalElGridN = Math.max(1, Math.min(64, parseInt(elModalGridNInput.value, 10) || 4));
    elModalGridNInput.value = String(modalElGridN);
    modalElSelection = [];
    modalElAnchorCell = null;
    for (let r = 0; r < modalElGridN; r++) {
      modalElSelection[r] = [];
      for (let c = 0; c < modalElGridN; c++) modalElSelection[r][c] = false;
    }
    elementModalCanvas.style.gridTemplateColumns = `repeat(${modalElGridN}, ${CELL_EL}px)`;
    elementModalCanvas.innerHTML = '';
    for (let r = 0; r < modalElGridN; r++) {
      for (let c = 0; c < modalElGridN; c++) {
        const cell = document.createElement('div');
        cell.className = 'cell';
        cell.dataset.r = String(r);
        cell.dataset.c = String(c);
        cell.addEventListener('click', () => {
          modalElSelection[r][c] = !modalElSelection[r][c];
          if (!modalElSelection[r][c] && modalElAnchorCell && modalElAnchorCell.r === r && modalElAnchorCell.c === c) {
            modalElAnchorCell = null;
          }
          cell.classList.toggle('on', modalElSelection[r][c]);
          repaintCanvasAnchorMark(elementModalCanvas, modalElAnchorCell);
          updateModalElStatus();
          refreshModalElementCanvasPreview();
        });
        cell.addEventListener('contextmenu', (e) => {
          e.preventDefault();
          if (!modalElSelection[r][c]) {
            showToast('请先左键选中该格，再右键设为锚点', 'err');
            return;
          }
          modalElAnchorCell = { r, c };
          repaintCanvasAnchorMark(elementModalCanvas, modalElAnchorCell);
          updateModalElStatus();
        });
        elementModalCanvas.appendChild(cell);
      }
    }
    updateModalElStatus();
    refreshModalElementCanvasPreview();
  }

  function openElementEditModal(el) {
    const fresh = elements.find((x) => x.id === el.id);
    if (!fresh || !fresh.id) return;
    modalEditElementId = fresh.id;
    const n = gridNForElement(fresh);
    if (elModalGridNInput) elModalGridNInput.value = String(n);
    buildModalElementCanvas();
    (fresh.cells || []).forEach(({ r, c }) => {
      if (r >= 0 && r < modalElGridN && c >= 0 && c < modalElGridN) {
        modalElSelection[r][c] = true;
        const cell = elementModalCanvas.querySelector('.cell[data-r="' + r + '"][data-c="' + c + '"]');
        if (cell) cell.classList.add('on');
      }
    });
    const a = anchorForElement(fresh);
    modalElAnchorCell = { r: a.r, c: a.c };
    repaintCanvasAnchorMark(elementModalCanvas, modalElAnchorCell);
    if (elModalName) elModalName.value = fresh.name || '';
    if (elModalType) elModalType.value = fresh.type || '';
    if (elModalColorInput) elModalColorInput.value = getElementColor(fresh);
    if (elModalDirection) elModalDirection.value = getDirection(fresh);
    if (elModalHasParamInput) elModalHasParamInput.checked = !!fresh.hasParam;
    modalElImageDataUrl = fresh.image ? String(fresh.image) : '';
    if (elModalImageInput) elModalImageInput.value = '';
    if (elModalImagePreview) {
      elModalImagePreview.innerHTML = modalElImageDataUrl ? '<img src="' + modalElImageDataUrl + '" alt="预览" />' : '';
    }
    updateModalElStatus();
    refreshModalElementCanvasPreview();
    if (elementEditDialog) elementEditDialog.hidden = false;
    if (elModalName) {
      try {
        elModalName.focus();
        elModalName.select();
      } catch (e) {
        /* ignore */
      }
    }
  }

  function loadElementsFromStorage() {
    try {
      const raw = localStorage.getItem(LOCAL_STORAGE_KEY);
      if (!raw) return;
      const data = JSON.parse(raw);
      const list = data.elements || data;
      if (!Array.isArray(list)) return;
      elements = list.filter((x) => x.id && x.cells && Array.isArray(x.cells)).map(normalizeElement);
    } catch (e) {
      console.warn('loadElementsFromStorage', e);
    }
  }

  function mapStateSnapshot() {
    return JSON.stringify({
      mapW,
      mapH,
      mapName: mapName && mapName.value != null ? mapName.value : '',
      placements: placements.map((p) => {
        const el = elements.find((e) => e.id === p.elementId);
        const rot = p.rotation || 0;
        return {
          id: p.id,
          elementId: p.elementId,
          row: p.row,
          col: p.col,
          rotation: rot,
          direction: effectivePlacementDirection(el, rot),
          param: typeof p.param === 'string' ? p.param : '',
        };
      }),
      activeMapId,
      elements: elements.map((e) => JSON.parse(JSON.stringify(e))),
    });
  }

  function applyMapSnapshot(json) {
    const s = JSON.parse(json);
    mapW = s.mapW;
    mapH = s.mapH;
    if (mapGridWInput) mapGridWInput.value = String(mapW);
    if (mapGridHInput) mapGridHInput.value = String(mapH);
    if (mapName) mapName.value = s.mapName || '';
    placements = (s.placements || []).map((p) => normalizePlacementRow(p));
    placements.forEach((p) => {
      if (typeof p.id === 'string' && /^\d+$/.test(p.id)) p.id = parseInt(p.id, 10);
    });
    syncNextPlacementIdFromPlacements();
    activeMapId = s.activeMapId != null ? s.activeMapId : null;
    if (s.elements && Array.isArray(s.elements)) {
      elements = s.elements.map((e) => normalizeElement(e));
      persistElements();
      renderElementList();
      renderMapElementList();
    }
    selectedElementId = null;
    selectedPlacementId = null;
    pendingRotation = 0;
    placementPivotRel = null;
    renderMap();
    renderMapList();
    updateUndoRedoButtons();
  }

  function markMapDirtyBaseline() {
    mapEditBaseline = mapStateSnapshot();
    updateSaveToLibraryButton();
  }

  function isMapDirty() {
    if (mapEditBaseline == null) return false;
    return mapEditBaseline !== mapStateSnapshot();
  }

  function updateSaveToLibraryButton() {
    if (!btnSaveMapLibrary) return;
    btnSaveMapLibrary.disabled = !isMapDirty();
  }

  function pushMapHistory() {
    if (mapHistoryRestoring) return;
    mapUndoStack.push(mapStateSnapshot());
    if (mapUndoStack.length > MAX_MAP_UNDO) mapUndoStack.shift();
    mapRedoStack.length = 0;
    updateUndoRedoButtons();
  }

  function updateUndoRedoButtons() {
    if (btnMapUndo) btnMapUndo.disabled = mapUndoStack.length === 0;
    if (btnMapRedo) btnMapRedo.disabled = mapRedoStack.length === 0;
  }

  function undoMap() {
    if (!mapUndoStack.length) return;
    mapHistoryRestoring = true;
    mapRedoStack.push(mapStateSnapshot());
    const prev = mapUndoStack.pop();
    applyMapSnapshot(prev);
    mapHistoryRestoring = false;
    updateUndoRedoButtons();
    showToast('已撤销', 'ok');
  }

  function redoMap() {
    if (!mapRedoStack.length) return;
    mapHistoryRestoring = true;
    mapUndoStack.push(mapStateSnapshot());
    const next = mapRedoStack.pop();
    applyMapSnapshot(next);
    mapHistoryRestoring = false;
    updateUndoRedoButtons();
    showToast('已重做', 'ok');
  }

  function hideMapUnsavedDialog() {
    if (mapUnsavedDialog) mapUnsavedDialog.hidden = true;
    pendingMapLoadId = null;
    pendingImportPayload = null;
  }

  function mergeMapIntoEditor(data) {
    if (!data.width || !data.height) throw new Error('缺少宽高');
    mapW = data.width;
    mapH = data.height;
    if (mapGridWInput) mapGridWInput.value = String(mapW);
    if (mapGridHInput) mapGridHInput.value = String(mapH);
    mapName.value = data.name || '';
    const incoming = data.elements || [];
    const byId = new Map(elements.map((e) => [e.id, e]));
    incoming.forEach((e) => byId.set(e.id, normalizeElement(e)));
    elements = Array.from(byId.values());
    placements = (data.placements || []).map((p) => normalizePlacementRow(p));
    migratePlacementIdsIfNeeded();
    selectedElementId = null;
    selectedPlacementId = null;
    pendingRotation = 0;
    placementPivotRel = null;
    persistElements();
    renderElementList();
    renderMapElementList();
    renderMap();
    markMapDirtyBaseline();
  }

  function loadMapLibraryFromStorage() {
    try {
      const raw = localStorage.getItem(LOCAL_STORAGE_MAPS_KEY);
      if (!raw) return;
      const data = JSON.parse(raw);
      const list = data.maps || data;
      if (!Array.isArray(list)) return;
      savedMaps = list.filter((m) => m && m.id && m.width && m.height);
    } catch (e) {
      console.warn('loadMapLibraryFromStorage', e);
    }
  }

  function persistMapLibrary() {
    try {
      localStorage.setItem(LOCAL_STORAGE_MAPS_KEY, JSON.stringify({ version: VERSION, maps: savedMaps }));
    } catch (e) {
      console.warn('persistMapLibrary', e);
    }
  }

  function loadMapViewPrefs() {
    try {
      const raw = localStorage.getItem(LOCAL_STORAGE_MAP_VIEW_PREFS_KEY);
      if (!raw) return;
      const o = JSON.parse(raw);
      if (typeof o !== 'object' || !o) return;
      if (typeof o.showAnchors === 'boolean' && mapShowAnchorToggle) mapShowAnchorToggle.checked = o.showAnchors;
      if (typeof o.showPlacementIds === 'boolean' && mapShowPlacementIdToggle) mapShowPlacementIdToggle.checked = o.showPlacementIds;
      if (typeof o.showPlacementParams === 'boolean' && mapShowPlacementParamToggle) mapShowPlacementParamToggle.checked = o.showPlacementParams;
      showMapAnchors = mapShowAnchorToggle ? !!mapShowAnchorToggle.checked : showMapAnchors;
      showPlacementIds = mapShowPlacementIdToggle ? !!mapShowPlacementIdToggle.checked : showPlacementIds;
      showPlacementParams = mapShowPlacementParamToggle ? !!mapShowPlacementParamToggle.checked : showPlacementParams;
    } catch (e) {
      console.warn('loadMapViewPrefs', e);
    }
  }

  function persistMapViewPrefs() {
    try {
      localStorage.setItem(
        LOCAL_STORAGE_MAP_VIEW_PREFS_KEY,
        JSON.stringify({
          showAnchors: !!(mapShowAnchorToggle && mapShowAnchorToggle.checked),
          showPlacementIds: !!(mapShowPlacementIdToggle && mapShowPlacementIdToggle.checked),
          showPlacementParams: !!(mapShowPlacementParamToggle && mapShowPlacementParamToggle.checked),
        })
      );
    } catch (e) {
      console.warn('persistMapViewPrefs', e);
    }
  }

  function idbOpenFs() {
    return new Promise((resolve, reject) => {
      const req = indexedDB.open(IDB_FS_NAME, 1);
      req.onupgradeneeded = () => {
        const db = req.result;
        if (!db.objectStoreNames.contains(IDB_FS_STORE)) {
          db.createObjectStore(IDB_FS_STORE);
        }
      };
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error);
    });
  }

  function idbPutFs(key, value) {
    return idbOpenFs().then(
      (db) =>
        new Promise((resolve, reject) => {
          const tx = db.transaction(IDB_FS_STORE, 'readwrite');
          tx.objectStore(IDB_FS_STORE).put(value, key);
          tx.oncomplete = () => resolve();
          tx.onerror = () => reject(tx.error);
        })
    );
  }

  function idbGetFs(key) {
    return idbOpenFs().then(
      (db) =>
        new Promise((resolve, reject) => {
          const tx = db.transaction(IDB_FS_STORE, 'readonly');
          const r = tx.objectStore(IDB_FS_STORE).get(key);
          r.onsuccess = () => resolve(r.result);
          r.onerror = () => reject(r.error);
        })
    );
  }

  async function loadWorkspaceExportHandleFromIdb() {
    if (typeof indexedDB === 'undefined') return;
    try {
      const h = await idbGetFs(IDB_WORKSPACE_EXPORT_KEY);
      if (h && typeof h.createWritable === 'function') {
        workspaceExportFileHandle = h;
      }
    } catch (e) {
      console.warn('loadWorkspaceExportHandleFromIdb', e);
    }
  }

  async function loadBatchExportDirHandleFromIdb() {
    if (typeof indexedDB === 'undefined') return;
    try {
      const h = await idbGetFs(IDB_BATCH_EXPORT_DIR_KEY);
      if (h && typeof h.getFileHandle === 'function') {
        batchExportDirHandle = h;
      }
    } catch (e) {
      console.warn('loadBatchExportDirHandleFromIdb', e);
    }
  }

  async function exportWorkspacePortable() {
    try {
      const payload = {
        version: VERSION,
        kind: WORKSPACE_KIND,
        exportedAt: Date.now(),
        elements: JSON.parse(JSON.stringify(elements)),
        maps: JSON.parse(JSON.stringify(savedMaps)),
        session: JSON.parse(mapStateSnapshot()),
      };
      const json = JSON.stringify(payload, null, 2);
      const blob = new Blob([json], { type: 'application/json' });

      async function writeToHandle(handle) {
        const writable = await handle.createWritable();
        await writable.write(json);
        await writable.close();
      }

      if (workspaceExportFileHandle && typeof workspaceExportFileHandle.createWritable === 'function') {
        try {
          let perm = await workspaceExportFileHandle.queryPermission({ mode: 'readwrite' });
          if (perm !== 'granted') {
            perm = await workspaceExportFileHandle.requestPermission({ mode: 'readwrite' });
          }
          if (perm === 'granted') {
            await writeToHandle(workspaceExportFileHandle);
            showToast('已覆盖关联的 workspace.json 文件', 'ok');
            return;
          }
        } catch (e) {
          console.warn('workspace export overwrite', e);
          workspaceExportFileHandle = null;
        }
      }

      if (typeof window.showSaveFilePicker === 'function') {
        try {
          const handle = await window.showSaveFilePicker({
            suggestedName: 'workspace.json',
            types: [
              {
                description: 'JSON',
                accept: { 'application/json': ['.json'] },
              },
            ],
          });
          await writeToHandle(handle);
          workspaceExportFileHandle = handle;
          try {
            await idbPutFs(IDB_WORKSPACE_EXPORT_KEY, handle);
          } catch (e) {
            console.warn('idbPutFs workspace handle', e);
          }
          showToast('已保存；下次导出将直接覆盖该文件', 'ok');
          return;
        } catch (e) {
          if (e && e.name === 'AbortError') {
            showToast('已取消保存', 'err');
            return;
          }
        }
      }

      downloadBlob(blob, 'workspace.json');
      showToast('已下载 workspace.json（当前环境无法直接写入磁盘；请手动放到 data/）', 'ok');
    } catch (e) {
      showToast('导出失败：' + (e && e.message ? e.message : String(e)), 'err');
    }
  }

  function isStorageEmpty() {
    try {
      if (localStorage.getItem(LOCAL_STORAGE_KEY)) return false;
      if (localStorage.getItem(LOCAL_STORAGE_MAPS_KEY)) return false;
    } catch (e) {
      console.warn('isStorageEmpty', e);
    }
    return true;
  }

  /**
   * 完整工作台：元素列表 + 地图列表 + 当前编辑会话（尺寸、放置、选中地图 id）。
   * @param {object} data
   * @param {{ silent?: boolean, skipConfirm?: boolean, fromBundle?: boolean }} [opts]
   */
  function applyWorkspaceImport(data, opts) {
    opts = opts || {};
    const silent = opts.silent;
    const skipConfirm = opts.skipConfirm;
    const fromBundle = opts.fromBundle;
    if (!data || typeof data !== 'object') throw new Error('无效数据');
    if (!skipConfirm && !silent && !isStorageEmpty()) {
      if (!confirm('将用文件中的工作台覆盖当前浏览器中的元素与地图列表，是否继续？')) {
        return;
      }
    }
    const els = data.elements;
    const maps = data.maps;
    if (!Array.isArray(els)) throw new Error('缺少 elements 数组');
    if (!Array.isArray(maps)) throw new Error('缺少 maps 数组');
    mapUndoStack = [];
    mapRedoStack = [];
    closeElementEditModal();
    elements = els.filter((x) => x.id && x.cells && Array.isArray(x.cells)).map(normalizeElement);
    savedMaps = maps.filter((m) => m && m.id && m.width && m.height);
    persistElements();
    persistMapLibrary();
    renderElementList();
    renderMapElementList();
    if (data.session && typeof data.session === 'object') {
      applyMapSnapshot(JSON.stringify(data.session));
      migratePlacementIdsIfNeeded();
      renderMap();
    } else if (savedMaps.length) {
      const m = savedMaps[0];
      activeMapId = m.id;
      mergeMapIntoEditor({
        name: m.name,
        width: m.width,
        height: m.height,
        elements: m.elements || [],
        placements: m.placements || [],
      });
    } else {
      activeMapId = null;
      placements = [];
      mapW = Math.max(1, Math.min(128, parseInt(mapGridWInput && mapGridWInput.value, 10) || 14));
      mapH = Math.max(1, Math.min(128, parseInt(mapGridHInput && mapGridHInput.value, 10) || 20));
      if (mapGridWInput) mapGridWInput.value = String(mapW);
      if (mapGridHInput) mapGridHInput.value = String(mapH);
      if (mapName) mapName.value = '';
      selectedPlacementId = null;
      pendingRotation = 0;
      placementPivotRel = null;
      renderMap();
      renderMapList();
    }
    markMapDirtyBaseline();
    updateUndoRedoButtons();
    updateSaveToLibraryButton();
    if (fromBundle && silent) showToast('已从 data/workspace.json 加载工作台', 'ok');
    else if (!silent) showToast('工作台已加载', 'ok');
  }

  async function tryLoadBundledWorkspace() {
    if (location.protocol === 'file:') return false;
    if (!isStorageEmpty()) return false;
    try {
      const res = await fetch(WORKSPACE_JSON_PATH + '?t=' + Date.now(), { cache: 'no-store' });
      if (!res.ok) return false;
      const data = await res.json();
      if (!data || typeof data !== 'object') return false;
      applyWorkspaceImport(data, { silent: true, skipConfirm: true, fromBundle: true });
      return true;
    } catch (e) {
      console.warn('tryLoadBundledWorkspace', e);
      return false;
    }
  }

  function fmtMapTime(ts) {
    if (!ts) return '';
    try {
      return new Date(ts).toLocaleString('zh-CN', { hour12: false });
    } catch (e) {
      return '';
    }
  }

  function saveMapToLibrary(asNew) {
    if (asNew) activeMapId = null;
    const name = mapName.value.trim() || '未命名地图';
    const usedIds = new Set(placements.map((p) => p.elementId));
    const mapElements = elements.filter((e) => usedIds.has(e.id));
    const entryId = activeMapId || uid();
    const entry = {
      id: entryId,
      name,
      width: mapW,
      height: mapH,
      elements: mapElements,
      placements: placements.map((p) => {
        const el = elements.find((e) => e.id === p.elementId);
        const rot = p.rotation || 0;
        return {
          id: p.id,
          elementId: p.elementId,
          row: p.row,
          col: p.col,
          rotation: rot,
          direction: effectivePlacementDirection(el, rot),
          param: typeof p.param === 'string' ? p.param : '',
        };
      }),
      updatedAt: Date.now(),
    };
    const idx = savedMaps.findIndex((m) => m.id === entry.id);
    if (idx >= 0) savedMaps[idx] = entry;
    else savedMaps.unshift(entry);
    activeMapId = entry.id;
    persistMapLibrary();
    renderMapList();
    markMapDirtyBaseline();
    let msg = '已保存到地图列表';
    if (asNew) msg = '已另存为新地图';
    else if (idx >= 0) msg = '已更新地图列表中的该项';
    showToast(msg, 'ok');
  }

  function applyLibraryMapLoad(id) {
    const m = savedMaps.find((x) => x.id === id);
    if (!m) return;
    try {
      pushMapHistory();
      // 须在 merge 之前设置，否则 markMapDirtyBaseline() 会写入旧的 activeMapId，导致误判「有未保存更改」
      activeMapId = m.id;
      mergeMapIntoEditor({
        name: m.name,
        width: m.width,
        height: m.height,
        elements: m.elements || [],
        placements: m.placements || [],
      });
      mapStatus.textContent = '已加载「' + m.name + '」';
      renderMapList();
      showToast('已加载「' + m.name + '」', 'ok');
    } catch (e) {
      showToast('加载失败：' + e.message, 'err');
    }
  }

  function loadMapFromLibrary(id) {
    const m = savedMaps.find((x) => x.id === id);
    if (!m) return;
    if (mapHistoryRestoring) {
      applyLibraryMapLoad(id);
      return;
    }
    if (!isMapDirty()) {
      applyLibraryMapLoad(id);
      return;
    }
    pendingMapLoadId = id;
    pendingImportPayload = null;
    if (mapUnsavedDialog) mapUnsavedDialog.hidden = false;
  }

  function deleteMapFromLibrary(id) {
    savedMaps = savedMaps.filter((m) => m.id !== id);
    if (activeMapId === id) activeMapId = null;
    persistMapLibrary();
    renderMapList();
    showToast('已从列表删除', 'ok');
  }

  /** 按地图名称排序（仅用于展示与批量导出初始顺序，不改变 localStorage 内数组顺序） */
  function sortedSavedMapsByName() {
    return savedMaps.slice().sort((a, b) => {
      const na = String(a.name != null ? a.name : '').trim();
      const nb = String(b.name != null ? b.name : '').trim();
      const cmp = na.localeCompare(nb, 'zh-CN');
      if (cmp !== 0) return cmp;
      return String(a.id).localeCompare(String(b.id));
    });
  }

  function renderMapList() {
    if (!mapList) return;
    mapList.innerHTML = '';
    if (!savedMaps.length) {
      const empty = document.createElement('li');
      empty.className = 'map-list-empty';
      empty.textContent = '暂无已保存的地图，编辑场景后在下方工具栏点击「保存当前地图到列表」。';
      mapList.appendChild(empty);
      return;
    }
    sortedSavedMapsByName().forEach((m) => {
      const li = document.createElement('li');
      li.className = 'map-list-item' + (m.id === activeMapId ? ' selected' : '');
      li.dataset.mapId = m.id;

      const meta = document.createElement('div');
      meta.className = 'map-list-meta';
      const n = (m.placements || []).length;
      meta.innerHTML =
        '<strong>' +
        escapeHtml(m.name) +
        '</strong><span>' +
        m.width +
        '×' +
        m.height +
        ' · ' +
        n +
        ' 个实例 · ' +
        escapeHtml(fmtMapTime(m.updatedAt)) +
        '</span>';

      const btns = document.createElement('div');
      btns.className = 'map-list-btns';
      const loadBtn = document.createElement('button');
      loadBtn.type = 'button';
      loadBtn.textContent = '加载';
      loadBtn.addEventListener('click', () => loadMapFromLibrary(m.id));
      const delBtn = document.createElement('button');
      delBtn.type = 'button';
      delBtn.className = 'danger';
      delBtn.textContent = '删除';
      delBtn.addEventListener('click', () => {
        if (confirm('确定从列表中删除「' + m.name + '」？')) deleteMapFromLibrary(m.id);
      });
      btns.appendChild(loadBtn);
      btns.appendChild(delBtn);

      li.appendChild(meta);
      li.appendChild(btns);
      mapList.appendChild(li);
    });
  }

  function isBatchExportDialogOpen() {
    return batchExportDialog && !batchExportDialog.hidden;
  }

  function closeBatchExportDialog() {
    if (batchExportDialog) batchExportDialog.hidden = true;
    batchExportDragId = null;
  }

  function reorderBatchExport(fromId, toId) {
    if (!fromId || !toId || fromId === toId) return;
    const a = batchExportOrderIds.filter((x) => x !== fromId);
    const idx = a.indexOf(toId);
    if (idx < 0) return;
    a.splice(idx, 0, fromId);
    batchExportOrderIds = a;
  }

  function renderBatchExportList() {
    if (!batchExportMapList) return;
    batchExportMapList.innerHTML = '';
    batchExportOrderIds.forEach((id, index) => {
      const m = savedMaps.find((x) => x.id === id);
      if (!m) return;
      const li = document.createElement('li');
      li.className = 'batch-export-map-item';
      li.dataset.mapId = String(id);
      li.draggable = true;
      li.setAttribute('aria-grabbed', 'false');

      const ord = document.createElement('span');
      ord.className = 'batch-export-order';
      ord.textContent = String(index + 1);

      const cb = document.createElement('input');
      cb.type = 'checkbox';
      cb.checked = !!batchExportChecked[id];
      cb.title = '导出此项';
      cb.addEventListener('click', (e) => {
        e.stopPropagation();
      });
      cb.addEventListener('change', () => {
        batchExportChecked[id] = !!cb.checked;
      });

      const meta = document.createElement('div');
      meta.className = 'batch-export-map-meta';
      const n = (m.placements || []).length;
      meta.innerHTML =
        '<strong>' +
        escapeHtml(m.name) +
        '</strong><span>' +
        m.width +
        '×' +
        m.height +
        ' · ' +
        n +
        ' 实例 · ' +
        escapeHtml(fmtMapTime(m.updatedAt)) +
        '</span>';

      const dragHint = document.createElement('span');
      dragHint.className = 'batch-export-drag-hint';
      dragHint.textContent = '↕';

      li.appendChild(ord);
      li.appendChild(cb);
      li.appendChild(meta);
      li.appendChild(dragHint);

      li.addEventListener('dragstart', (e) => {
        batchExportDragId = String(id);
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', String(id));
        li.classList.add('batch-export-item-dragging');
        li.setAttribute('aria-grabbed', 'true');
      });
      li.addEventListener('dragend', () => {
        li.classList.remove('batch-export-item-dragging');
        li.setAttribute('aria-grabbed', 'false');
        batchExportMapList.querySelectorAll('.batch-export-item-dragover').forEach((el) => el.classList.remove('batch-export-item-dragover'));
        batchExportDragId = null;
      });
      li.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        li.classList.add('batch-export-item-dragover');
      });
      li.addEventListener('dragleave', () => {
        li.classList.remove('batch-export-item-dragover');
      });
      li.addEventListener('drop', (e) => {
        e.preventDefault();
        li.classList.remove('batch-export-item-dragover');
        const fromId = e.dataTransfer.getData('text/plain');
        const toId = String(id);
        reorderBatchExport(fromId, toId);
        renderBatchExportList();
      });

      batchExportMapList.appendChild(li);
    });
  }

  function openBatchExportDialog() {
    if (!savedMaps.length) {
      showToast('暂无已保存的地图', 'err');
      return;
    }
    batchExportOrderIds = sortedSavedMapsByName().map((m) => m.id);
    batchExportChecked = {};
    savedMaps.forEach((m) => {
      batchExportChecked[m.id] = true;
    });
    renderBatchExportList();
    if (batchExportDialog) batchExportDialog.hidden = false;
  }

  /** 与编辑器内「保存地图到文件」一致：对副本规范化实例 id */
  function normalizePlacementsCopyForExport(placementsIn) {
    const arr = JSON.parse(JSON.stringify(placementsIn || []));
    if (arr.some((p) => !isPlainNumericId(p.id))) {
      arr.forEach((p, i) => {
        p.id = i;
      });
    } else {
      arr.forEach((p) => {
        if (typeof p.id === 'string' && /^\d+$/.test(p.id)) p.id = parseInt(p.id, 10);
      });
    }
    return arr;
  }

  /** 从已保存地图条目生成单张 .map.json 的 payload（与 btnSaveMap 结构一致） */
  function buildMapJsonPayloadFromSavedMap(m) {
    const name = (m.name && String(m.name).trim()) || '未命名地图';
    const rawEls = Array.isArray(m.elements) ? m.elements : [];
    const elems = rawEls.map((e) => normalizeElement(e));
    const byId = new Map(elems.map((e) => [e.id, e]));
    const placementsNorm = normalizePlacementsCopyForExport(m.placements);
    const usedIds = new Set(placementsNorm.map((p) => p.elementId));
    const mapElements = elems.filter((e) => usedIds.has(e.id));
    return {
      version: VERSION,
      name,
      width: m.width,
      height: m.height,
      elements: mapElements,
      placements: placementsNorm.map((p) => {
        const el = byId.get(p.elementId);
        const rot = p.rotation || 0;
        return {
          elementId: p.elementId,
          row: p.row,
          col: p.col,
          id: p.id,
          rotation: rot,
          direction: effectivePlacementDirection(el, rot),
          param: typeof p.param === 'string' ? p.param : '',
        };
      }),
    };
  }

  /** 按列表顺序生成文件名；同名时加 id 后缀，避免单次导出互相覆盖 */
  function batchExportFilenamesForOrderedMaps(maps) {
    const seen = Object.create(null);
    return maps.map((m, i) => {
      const base = sanitizeFilename((m.name && String(m.name).trim()) || '未命名地图');
      let fname = base + '.json';
      if (seen[fname]) {
        const idSuffix = String(m.id).replace(/[/\\?%*:|"<>]/g, '-').slice(0, 36);
        fname = base + '-' + idSuffix + '.json';
      }
      if (seen[fname]) fname = base + '-n' + i + '.json';
      seen[fname] = true;
      return fname;
    });
  }

  async function performBatchExport() {
    const ordered = [];
    batchExportOrderIds.forEach((id) => {
      if (!batchExportChecked[id]) return;
      const m = savedMaps.find((x) => x.id === id);
      if (m) ordered.push(m);
    });
    if (!ordered.length) {
      showToast('请至少勾选一张地图', 'err');
      return;
    }
    try {
      const fnames = batchExportFilenamesForOrderedMaps(ordered);
      const items = ordered.map((m, i) => ({
        fname: fnames[i],
        json: JSON.stringify(buildMapJsonPayloadFromSavedMap(m), null, 2),
      }));

      async function writeItemsToDirectory(dirHandle) {
        for (let i = 0; i < items.length; i++) {
          const fh = await dirHandle.getFileHandle(items[i].fname, { create: true });
          const w = await fh.createWritable();
          await w.write(items[i].json);
          await w.close();
        }
      }

      if (typeof window.showDirectoryPicker === 'function') {
        let dirHandle = batchExportDirHandle;
        if (dirHandle && typeof dirHandle.getFileHandle === 'function') {
          try {
            let perm = await dirHandle.queryPermission({ mode: 'readwrite' });
            if (perm !== 'granted') perm = await dirHandle.requestPermission({ mode: 'readwrite' });
            if (perm === 'granted') {
              await writeItemsToDirectory(dirHandle);
              closeBatchExportDialog();
              showToast('已写入 ' + items.length + ' 个 .json（同名已覆盖）', 'ok');
              return;
            }
          } catch (e) {
            console.warn('batch export dir reuse', e);
            batchExportDirHandle = null;
          }
        }
        try {
          dirHandle = await window.showDirectoryPicker();
          batchExportDirHandle = dirHandle;
          await idbPutFs(IDB_BATCH_EXPORT_DIR_KEY, dirHandle);
          await writeItemsToDirectory(dirHandle);
          closeBatchExportDialog();
          showToast('已写入 ' + items.length + ' 个 .json；下次导出可复用该文件夹', 'ok');
          return;
        } catch (e) {
          if (e && e.name === 'AbortError') {
            showToast('已取消', 'err');
            return;
          }
          throw e;
        }
      }

      for (let i = 0; i < items.length; i++) {
        const blob = new Blob([items[i].json], { type: 'application/json' });
        downloadBlob(blob, items[i].fname);
        if (i < items.length - 1) await new Promise((r) => setTimeout(r, 150));
      }
      closeBatchExportDialog();
      showToast(
        '已触发 ' + items.length + ' 个文件下载（请自行放入同一文件夹）。若浏览器拦截多文件下载，请允许本站下载后重试。',
        'ok'
      );
    } catch (e) {
      showToast('导出失败：' + (e && e.message ? e.message : String(e)), 'err');
    }
  }

  function refreshLiveElementPreview() {
    const placeholder = $('el-live-preview-placeholder');
    const inner = $('el-live-preview-inner');
    const grid = $('el-live-preview-grid');
    const labelEl = $('el-live-preview-label');
    if (!placeholder || !inner || !grid || !labelEl) return;

    const normalized = normalizeCells(elSelection, elGridN);
    if (!normalized.length) {
      placeholder.hidden = false;
      inner.hidden = true;
      return;
    }

    placeholder.hidden = true;
    inner.hidden = false;

    const bb = bboxOfCells(normalized);
    const bh = bb.h;
    const bw = bb.w;

    const fakeEl = {
      cells: normalized,
      image: elImageDataUrl || '',
      color: elColorInput ? elColorInput.value : '#4488cc',
      name: elName && elName.value.trim() ? elName.value.trim() : '未命名元素',
      direction: elDirectionInput && elDirectionInput.value ? elDirectionInput.value : 'down',
    };

    renderElementFootprintIntoGrid(grid, fakeEl, { cellSize: CELL_MAP });

    if (isColorOnlyElement(fakeEl)) {
      labelEl.hidden = false;
      labelEl.className = 'placement-label el-preview-label placement-label-stack';
      labelEl.innerHTML = '';
      const arr = document.createElement('span');
      arr.className = 'placement-arrow';
      arr.textContent = ARROW_CHARS[directionIndex(getDirection(fakeEl))];
      const nm = document.createElement('span');
      nm.className = 'placement-name';
      nm.textContent = fakeEl.name;
      labelEl.appendChild(arr);
      labelEl.appendChild(nm);
      const left = (bw * STEP_MAP - GAP) / 2;
      const top = (bh * STEP_MAP - GAP) / 2;
      labelEl.style.left = left + 'px';
      labelEl.style.top = top + 'px';
    } else {
      labelEl.hidden = true;
      labelEl.className = 'placement-label el-preview-label';
      labelEl.textContent = '';
    }
  }

  function cellsKeySet(cells, row, col) {
    const set = new Set();
    cells.forEach(({ r, c }) => set.add(`${row + r},${col + c}`));
    return set;
  }

  function occupiedByPlacements(excludePlacementId) {
    const set = new Set();
    placements.forEach((p) => {
      if (p.id == excludePlacementId) return;
      const el = elements.find((e) => e.id === p.elementId);
      if (!el) return;
      const rc = getCellsWithRotationForElement(el, p.rotation || 0);
      rc.forEach(({ r, c }) => set.add(`${p.row + r},${p.col + c}`));
    });
    return set;
  }

  function canPlace(element, row, col, excludePlacementId, rotation) {
    const rot = rotation != null ? rotation : 0;
    const occ = occupiedByPlacements(excludePlacementId);
    const rc = getCellsWithRotationForElement(element, rot);
    for (let i = 0; i < rc.length; i++) {
      const { r, c } = rc[i];
      const mr = row + r;
      const mc = col + c;
      if (mr < 0 || mc < 0 || mr >= mapH || mc >= mapW) return { ok: false, reason: '超出地图边界' };
      const k = `${mr},${mc}`;
      if (occ.has(k)) return { ok: false, reason: '与已有元素重叠' };
    }
    return { ok: true };
  }

  /** 放置预览：鼠标所在格即元素锚点格 */
  function canPlaceAtHover(element, hoverRow, hoverCol, excludePlacementId, rotation) {
    const a = topLeftFromAnchorCell(element, hoverRow, hoverCol, rotation);
    return canPlace(element, a.row, a.col, excludePlacementId, rotation);
  }

  function buildElementCanvas() {
    elGridN = Math.max(1, Math.min(64, parseInt(elGridNInput.value, 10) || 4));
    elGridNInput.value = String(elGridN);
    elSelection = [];
    elAnchorCell = null;
    for (let r = 0; r < elGridN; r++) {
      elSelection[r] = [];
      for (let c = 0; c < elGridN; c++) elSelection[r][c] = false;
    }
    elementCanvas.style.gridTemplateColumns = `repeat(${elGridN}, ${CELL_EL}px)`;
    elementCanvas.innerHTML = '';
    for (let r = 0; r < elGridN; r++) {
      for (let c = 0; c < elGridN; c++) {
        const cell = document.createElement('div');
        cell.className = 'cell';
        cell.dataset.r = String(r);
        cell.dataset.c = String(c);
        cell.addEventListener('click', () => {
          elSelection[r][c] = !elSelection[r][c];
          if (!elSelection[r][c] && elAnchorCell && elAnchorCell.r === r && elAnchorCell.c === c) {
            elAnchorCell = null;
          }
          cell.classList.toggle('on', elSelection[r][c]);
          repaintCanvasAnchorMark(elementCanvas, elAnchorCell);
          updateElStatus();
          refreshElementCanvasPreview();
        });
        cell.addEventListener('contextmenu', (e) => {
          e.preventDefault();
          if (!elSelection[r][c]) {
            showToast('请先左键选中该格，再右键设为锚点', 'err');
            return;
          }
          elAnchorCell = { r, c };
          repaintCanvasAnchorMark(elementCanvas, elAnchorCell);
          updateElStatus();
        });
        elementCanvas.appendChild(cell);
      }
    }
    updateElStatus();
    refreshElementCanvasPreview();
  }

  function refreshElementCanvasPreview() {
    const color = elColorInput ? elColorInput.value : '#4488cc';
    elementCanvas.querySelectorAll('.cell').forEach((cell) => {
      const isOn = cell.classList.contains('on');
      if (!elImageDataUrl && isOn) {
        cell.style.backgroundColor = hexToRgba(color, 0.55);
        cell.style.boxShadow = 'inset 0 0 0 2px ' + darkenHex(color, 0.5);
      } else if (isOn) {
        cell.style.backgroundColor = '';
        cell.style.boxShadow = '';
      } else {
        cell.style.backgroundColor = '';
        cell.style.boxShadow = '';
      }
    });
    refreshLiveElementPreview();
  }

  function updateElStatus() {
    const n = normalizeCells(elSelection, elGridN).length;
    if (!n) {
      elStatus.textContent = '请在画布上点选至少一格';
      return;
    }
    const a = elAnchorCell ? `，锚点 (${elAnchorCell.r},${elAnchorCell.c})` : '，请右键一个已选格子设为锚点';
    elStatus.textContent = `已选 ${n} 格${a}`;
  }

  function renderElementList() {
    elementList.innerHTML = '';
    elements.forEach((el) => {
      const li = document.createElement('li');
      li.className = 'element-item';
      li.draggable = true;
      li.dataset.elementId = el.id;

      li.appendChild(renderListItemPreview(el));

      const meta = document.createElement('div');
      meta.className = 'meta';
      meta.innerHTML =
        '<strong>' +
        escapeHtml(el.name) +
        '</strong><span>' +
        escapeHtml(el.type) +
        ' · ' +
        el.cells.length +
        ' 格' +
        (isColorOnlyElement(el) ? ' · 纯色' : '') +
        (el.hasParam ? ' · 参数' : '') +
        '</span>';

      const btnEdit = document.createElement('button');
      btnEdit.type = 'button';
      btnEdit.className = 'edit-el';
      btnEdit.textContent = '编辑';
      btnEdit.addEventListener('click', (e) => {
        e.stopPropagation();
        openElementEditModal(el);
      });

      const btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'remove';
      btn.textContent = '移除';
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        if (modalEditElementId === el.id) closeElementEditModal();
        elements = elements.filter((x) => x.id !== el.id);
        if (selectedElementId === el.id) selectedElementId = null;
        placements = placements.filter((p) => p.elementId !== el.id);
        if (selectedPlacementId && !placements.some((p) => p.id == selectedPlacementId)) selectedPlacementId = null;
        persistElements();
        renderElementList();
        renderMapElementList();
        renderMap();
      });

      const btnWrap = document.createElement('div');
      btnWrap.className = 'element-item-btns';
      btnWrap.appendChild(btnEdit);
      btnWrap.appendChild(btn);

      li.appendChild(meta);
      li.appendChild(btnWrap);

      li.addEventListener('dragstart', (e) => {
        e.dataTransfer.setData('application/x-element-id', el.id);
        e.dataTransfer.effectAllowed = 'copy';
      });
      elementList.appendChild(li);
    });
  }

  function escapeHtml(s) {
    const d = document.createElement('div');
    d.textContent = s;
    return d.innerHTML;
  }

  function renderMapElementList() {
    mapElementList.innerHTML = '';

    const cancelLi = document.createElement('li');
    cancelLi.className = 'element-item element-item-cancel' + (!selectedElementId ? ' selected' : '');
    cancelLi.draggable = false;
    cancelLi.innerHTML =
      '<div class="meta meta-cancel"><strong>取消选择</strong><span>移动场景中的元素</span></div>';
    cancelLi.addEventListener('dragstart', (e) => e.preventDefault());
    cancelLi.addEventListener('click', () => {
      selectedElementId = null;
      selectedPlacementId = null;
      pendingRotation = 0;
      placementPivotRel = null;
      renderMapElementList();
      renderMap();
      mapStatus.textContent = '移动模式：点击选中实例，拖拽或点空地移动；按 R 旋转';
      updateMapHoverLayer();
    });
    mapElementList.appendChild(cancelLi);

    elements.forEach((el) => {
      const li = document.createElement('li');
      li.className = 'element-item' + (selectedElementId === el.id ? ' selected' : '');
      li.draggable = true;
      li.dataset.elementId = el.id;

      li.appendChild(renderListItemPreview(el));

      const meta = document.createElement('div');
      meta.className = 'meta';
      meta.innerHTML =
        '<strong>' +
        escapeHtml(el.name) +
        '</strong><span>' +
        escapeHtml(el.type) +
        (el.hasParam ? ' · 参数' : '') +
        '</span>';

      li.appendChild(meta);

      li.addEventListener('click', () => {
        selectedElementId = el.id;
        selectedPlacementId = null;
        pendingRotation = 0;
        placementPivotRel = null;
        renderMapElementList();
        mapStatus.textContent = '已选中「' + el.name + '」，点击地图放置；按 R 旋转';
        updateMapHoverLayer();
      });
      li.addEventListener('dragstart', (e) => {
        e.dataTransfer.setData('application/x-element-id', el.id);
        e.dataTransfer.effectAllowed = 'copy';
      });
      mapElementList.appendChild(li);
    });
  }

  function mapCellsForPlacementAt(element, row, col, rotation) {
    const rc = getCellsWithRotationForElement(element, rotation || 0);
    return rc.map(({ r, c }) => ({ row: row + r, col: col + c }));
  }

  function neighborInFootprint(keySet, r, c) {
    if (r < 0 || c < 0 || r >= mapH || c >= mapW) return false;
    return keySet.has(`${r},${c}`);
  }

  const SVG_NS = 'http://www.w3.org/2000/svg';

  /** 非矩形多连块：SVG 扩展小格盖住 gap，并画外轮廓 */
  function appendPolyominoColorSvg(svgEl, p, el, keySet) {
    const fillCol = hexToRgba(getElementColor(el), 0.62);
    const strokeCol = darkenHex(getElementColor(el), 0.32);
    const g = document.createElementNS(SVG_NS, 'g');
    g.setAttribute(
      'class',
      'placement-fill placement-svg-group' + (p.id == selectedPlacementId ? ' placement-fill-selected' : '')
    );
    g.dataset.placementId = p.id;
    keySet.forEach((key) => {
      const parts = key.split(',');
      const r = parseInt(parts[0], 10);
      const c = parseInt(parts[1], 10);
      const hasE = neighborInFootprint(keySet, r, c + 1);
      const hasS = neighborInFootprint(keySet, r + 1, c);
      const x = c * STEP_MAP;
      const y = r * STEP_MAP;
      const w = CELL_MAP + (hasE ? GAP : 0);
      const h = CELL_MAP + (hasS ? GAP : 0);
      const rect = document.createElementNS(SVG_NS, 'rect');
      rect.setAttribute('x', String(x));
      rect.setAttribute('y', String(y));
      rect.setAttribute('width', String(w));
      rect.setAttribute('height', String(h));
      rect.setAttribute('fill', fillCol);
      rect.setAttribute('stroke', 'none');
      g.appendChild(rect);
    });
    keySet.forEach((key) => {
      const parts = key.split(',');
      const r = parseInt(parts[0], 10);
      const c = parseInt(parts[1], 10);
      const x0 = c * STEP_MAP;
      const y0 = r * STEP_MAP;
      const x1 = x0 + CELL_MAP;
      const y1 = y0 + CELL_MAP;
      function addLine(xA, yA, xB, yB) {
        const line = document.createElementNS(SVG_NS, 'line');
        line.setAttribute('x1', String(xA));
        line.setAttribute('y1', String(yA));
        line.setAttribute('x2', String(xB));
        line.setAttribute('y2', String(yB));
        line.setAttribute('stroke', strokeCol);
        line.setAttribute('stroke-width', '3');
        line.setAttribute('stroke-linecap', 'square');
        line.setAttribute('class', 'placement-svg-edge');
        g.appendChild(line);
      }
      if (!neighborInFootprint(keySet, r - 1, c)) addLine(x0, y0, x1, y0);
      if (!neighborInFootprint(keySet, r + 1, c)) addLine(x0, y1, x1, y1);
      if (!neighborInFootprint(keySet, r, c - 1)) addLine(x0, y0, x0, y1);
      if (!neighborInFootprint(keySet, r, c + 1)) addLine(x1, y0, x1, y1);
    });
    svgEl.appendChild(g);
  }

  /** 桌面宽屏下让右侧「地图列表」卡高度与左侧「场景」卡一致，避免侧栏过长或 sticky 盖住下方「可放置元素」 */
  function syncMapLibraryMaxHeight() {
    const scene = document.querySelector('.map-scene-card');
    const lib = document.querySelector('.map-library-card');
    if (!lib || !scene) return;
    if (window.matchMedia('(max-width: 959px)').matches) {
      lib.style.maxHeight = '';
      return;
    }
    if (!panelMap || panelMap.hidden) {
      lib.style.maxHeight = '';
      return;
    }
    lib.style.maxHeight = scene.offsetHeight + 'px';
  }

  function renderMap() {
    renderMapAxis();
    mapCanvas.style.gridTemplateColumns = `repeat(${mapW}, ${CELL_MAP}px)`;
    mapCanvas.style.gridTemplateRows = `repeat(${mapH}, ${CELL_MAP}px)`;
    mapCanvas.innerHTML = '';

    const placementKeys = new Map();
    placements.forEach((p) => {
      const el = elements.find((e) => e.id === p.elementId);
      if (!el) return;
      const rc = getCellsWithRotationForElement(el, p.rotation || 0);
      const set = new Set();
      rc.forEach(({ r: dr, c: dc }) => set.add(`${p.row + dr},${p.col + dc}`));
      placementKeys.set(p.id, set);
    });

    /** 纯色完整矩形：div 整块；非矩形（U/L 等）：SVG 扩展小格盖住 gap 并描边 */
    const rectFillIds = new Set();
    placements.forEach((p) => {
      const el = elements.find((e) => e.id === p.elementId);
      if (!el || !isColorOnlyElement(el)) return;
      const { norm } = normalizeFootprintCells(getCellsWithRotationForElement(el, p.rotation || 0));
      if (isFullRectangleFootprintNorm(norm)) rectFillIds.add(p.id);
    });

    const fillsWrap = document.createElement('div');
    fillsWrap.className = 'map-placement-fills-inner';
    fillsWrap.setAttribute('aria-hidden', 'true');
    placements.forEach((p) => {
      if (!rectFillIds.has(p.id)) return;
      const el = elements.find((e) => e.id === p.elementId);
      if (!el) return;
      const rot = p.rotation || 0;
      const rc = getCellsWithRotationForElement(el, rot);
      const { norm, minDr, minDc } = normalizeFootprintCells(rc);
      if (!isFullRectangleFootprintNorm(norm)) return;
      const bb = bboxOfCells(norm);
      const fill = document.createElement('div');
      fill.className = 'placement-fill' + (p.id == selectedPlacementId ? ' placement-fill-selected' : '');
      fill.dataset.placementId = p.id;
      fill.style.left = (p.col + minDc) * STEP_MAP + 'px';
      fill.style.top = (p.row + minDr) * STEP_MAP + 'px';
      fill.style.width = bb.w * STEP_MAP - GAP + 'px';
      fill.style.height = bb.h * STEP_MAP - GAP + 'px';
      const fillColor = getElementColor(el);
      fill.style.backgroundColor = hexToRgba(fillColor, 0.62);
      fill.style.border = '3px solid ' + darkenHex(fillColor, 0.32);
      fill.style.borderRadius = '4px';
      fill.style.boxSizing = 'border-box';
      fillsWrap.appendChild(fill);
    });
    const innerW = mapW * STEP_MAP - GAP;
    const innerH = mapH * STEP_MAP - GAP;
    const svgPoly = document.createElementNS(SVG_NS, 'svg');
    svgPoly.setAttribute('width', String(innerW));
    svgPoly.setAttribute('height', String(innerH));
    svgPoly.setAttribute('viewBox', '0 0 ' + innerW + ' ' + innerH);
    svgPoly.style.cssText = 'position:absolute;left:0;top:0;overflow:visible;pointer-events:none;z-index:0;';
    placements.forEach((p) => {
      const el = elements.find((e) => e.id === p.elementId);
      if (!el || !isColorOnlyElement(el)) return;
      if (rectFillIds.has(p.id)) return;
      const ks = placementKeys.get(p.id);
      if (ks) appendPolyominoColorSvg(svgPoly, p, el, ks);
    });
    fillsWrap.appendChild(svgPoly);
    fillsWrap.style.width = innerW + 'px';
    fillsWrap.style.height = innerH + 'px';
    mapCanvas.appendChild(fillsWrap);

    /** @type {Map<string, { element: object, placement: object }>} */
    const cellOwner = new Map();
    placements.forEach((p) => {
      const el = elements.find((e) => e.id === p.elementId);
      if (!el) return;
      mapCellsForPlacementAt(el, p.row, p.col, p.rotation || 0).forEach(({ row, col }) => {
        cellOwner.set(`${row},${col}`, { element: el, placement: p });
      });
    });

    const bboxCache = new Map();
    function getBboxForPlacement(el, rot) {
      const key = el.id + ':' + (rot || 0);
      if (bboxCache.has(key)) return bboxCache.get(key);
      const b = bboxOfCells(getCellsWithRotationForElement(el, rot || 0));
      bboxCache.set(key, b);
      return b;
    }

    for (let r = 0; r < mapH; r++) {
      for (let c = 0; c < mapW; c++) {
        const cell = document.createElement('div');
        cell.className = 'cell';
        cell.dataset.row = String(r);
        cell.dataset.col = String(c);
        const key = `${r},${c}`;
        const owner = cellOwner.get(key);
        if (owner) {
          const { element: el, placement: p } = owner;
          cell.dataset.placementId = p.id;
          const rot = p.rotation || 0;
          const keySet = placementKeys.get(p.id);
          const anchorAbs = anchorCellFromTopLeft(el, p.row, p.col, rot);
          if (showMapAnchors && anchorAbs.row === r && anchorAbs.col === c) {
            cell.classList.add('cell-anchor', 'map-anchor-cell');
          }
          const isColorOverlay = isColorOnlyElement(el);
          if (p.id == selectedPlacementId && !isColorOverlay) cell.classList.add('cell-placement-selected');
          if (!isColorOverlay) {
            cell.classList.add('has-image');
            const { w: bw, h: bh } = getBboxForPlacement(el, rot);
            const tw = bw * STEP_MAP - GAP;
            const th = bh * STEP_MAP - GAP;
            const dr = r - p.row;
            const dc = c - p.col;
            cell.style.backgroundImage = 'url(' + el.image + ')';
            cell.style.backgroundSize = tw + 'px ' + th + 'px';
            cell.style.backgroundPosition = -dc * STEP_MAP + 'px ' + -dr * STEP_MAP + 'px';
            if (keySet) {
              const outline = 'rgba(230, 237, 243, 0.78)';
              const bwPx = 2;
              if (!neighborInFootprint(keySet, r - 1, c)) cell.style.borderTop = bwPx + 'px solid ' + outline;
              if (!neighborInFootprint(keySet, r + 1, c)) cell.style.borderBottom = bwPx + 'px solid ' + outline;
              if (!neighborInFootprint(keySet, r, c - 1)) cell.style.borderLeft = bwPx + 'px solid ' + outline;
              if (!neighborInFootprint(keySet, r, c + 1)) cell.style.borderRight = bwPx + 'px solid ' + outline;
            }
          } else {
            cell.classList.add('cell-color-unified');
          }
        }

        mapCanvas.appendChild(cell);
      }
    }

    if (mapPlacementLabels) {
      mapPlacementLabels.innerHTML = '';
      const innerW = mapW * STEP_MAP - GAP;
      const innerH = mapH * STEP_MAP - GAP;
      mapPlacementLabels.style.width = innerW + 'px';
      mapPlacementLabels.style.height = innerH + 'px';
      placements.forEach((p) => {
        const el = elements.find((e) => e.id === p.elementId);
        if (!el) return;
        const rot = p.rotation || 0;
        const bb = bboxOfCells(getCellsWithRotationForElement(el, rot));
        const left = p.col * STEP_MAP + (bb.w * STEP_MAP - GAP) / 2;
        const top = p.row * STEP_MAP + (bb.h * STEP_MAP - GAP) / 2;

        // 颜色块：显示箭头+名称（原逻辑保留）
        if (isColorOnlyElement(el)) {
          const span = document.createElement('div');
          span.className = 'placement-label placement-label-stack';
          const arr = document.createElement('span');
          arr.className = 'placement-arrow';
          arr.textContent = effectiveArrowChar(el, rot);
          const nm = document.createElement('span');
          nm.className = 'placement-name';
          nm.textContent = el.name;
          span.appendChild(arr);
          span.appendChild(nm);
          span.style.left = left + 'px';
          span.style.top = top + 'px';
          mapPlacementLabels.appendChild(span);
        }

        // 所有实例：显示 id（小徽标，贴左上角），可由工具栏复选框关闭
        if (showPlacementIds) {
          const idBadge = document.createElement('div');
          idBadge.className = 'placement-id-badge';
          idBadge.textContent = '#' + String(p.id);
          idBadge.style.left = p.col * STEP_MAP + 4 + 'px';
          idBadge.style.top = p.row * STEP_MAP + 4 + 'px';
          mapPlacementLabels.appendChild(idBadge);
        }

        if (showPlacementParams && el.hasParam) {
          const paramText = typeof p.param === 'string' ? p.param.trim() : '';
          if (paramText) {
            const paramEl = document.createElement('div');
            paramEl.className = 'map-placement-param-label';
            paramEl.textContent = paramText;
            const leftParam = p.col * STEP_MAP + (bb.w * STEP_MAP - GAP) / 2;
            const topParam = p.row * STEP_MAP + (bb.h * STEP_MAP - GAP) - 4;
            paramEl.style.left = leftParam + 'px';
            paramEl.style.top = topParam + 'px';
            paramEl.style.maxWidth = Math.max(48, bb.w * STEP_MAP - GAP - 6) + 'px';
            mapPlacementLabels.appendChild(paramEl);
          }
        }
      });
    }

    updateMapHoverLayer();
    updateSaveToLibraryButton();
    requestAnimationFrame(syncMapLibraryMaxHeight);
  }

  let mapPlacementDrag = null;
  let suppressNextMapClick = false;

  function onMapPlacementDragMove(e) {
    if (!mapPlacementDrag) return;
    const dx = e.clientX - mapPlacementDrag.px;
    const dy = e.clientY - mapPlacementDrag.py;
    if (dx * dx + dy * dy > 49) {
      mapPlacementDrag.dragged = true;
      if (mapCanvas) mapCanvas.classList.add('map-dragging');
      updatePlacementDragPreview(e.clientX, e.clientY);
    }
  }

  function onMapPlacementDragEnd(e) {
    document.removeEventListener('mousemove', onMapPlacementDragMove);
    document.removeEventListener('mouseup', onMapPlacementDragEnd);
    if (mapCanvas) {
      mapCanvas.classList.remove('map-dragging');
      mapCanvas.style.cursor = '';
    }
    clearPlacementDragDimCells();
    const d = mapPlacementDrag;
    mapPlacementDrag = null;
    if (!d) return;
    if (d.dragged) suppressNextMapClick = true;
    updateMapHoverLayer();
    if (!d.dragged) return;
    const under = document.elementFromPoint(e.clientX, e.clientY);
    const cell = under && under.closest ? under.closest('.cell') : null;
    const p = placements.find((x) => x.id == d.pid);
    if (!p) return;
    const el = elements.find((e) => e.id === p.elementId);
    if (!el) return;
    if (!cell || !mapCanvas.contains(cell)) {
      showToast('请在地图区域内释放以完成移动', 'err');
      return;
    }
    const row = parseInt(cell.dataset.row, 10);
    const col = parseInt(cell.dataset.col, 10);
    const aMove = topLeftFromAnchorCell(el, row, col, p.rotation || 0);
    const check = canPlace(el, aMove.row, aMove.col, d.pid, p.rotation || 0);
    if (!check.ok) {
      showToast('无法移动：' + check.reason, 'err');
      return;
    }
    pushMapHistory();
    p.row = aMove.row;
    p.col = aMove.col;
    selectedPlacementId = null;
    renderMap();
    renderMapElementList();
    showToast('已移动', 'ok');
  }

  function onMapPlacementMouseDown(e) {
    if (e.button !== 0) return;
    if (selectedElementId) return;
    const cell = e.target.closest('.cell');
    if (!cell || !mapCanvas.contains(cell)) return;
    const pid = cell.dataset.placementId;
    if (!pid) return;
    mapPlacementDrag = { pid, px: e.clientX, py: e.clientY, dragged: false };
    e.preventDefault();
    document.addEventListener('mousemove', onMapPlacementDragMove);
    document.addEventListener('mouseup', onMapPlacementDragEnd);
  }

  function setupMapInteractions() {
    mapCanvas.addEventListener('mousedown', onMapPlacementMouseDown);
    mapCanvas.addEventListener('click', (e) => {
      if (suppressNextMapClick) {
        suppressNextMapClick = false;
        return;
      }
      const cell = e.target.closest('.cell');
      if (!cell || !mapCanvas.contains(cell)) return;
      const row = parseInt(cell.dataset.row, 10);
      const col = parseInt(cell.dataset.col, 10);
      const pid = cell.dataset.placementId;

      if (!selectedElementId) {
        if (pid) {
          const pl = placements.find((x) => x.id == pid);
          const elHit = pl && elements.find((e) => e.id === pl.elementId);
          if (elHit && elHit.hasParam) {
            selectedPlacementId = pid;
            renderMap();
            renderMapElementList();
            openPlacementParamDialog(pid);
            mapStatus.textContent = '可编辑参数；拖拽移动实例，按 R 旋转';
            return;
          }
          selectedPlacementId = pid;
          renderMap();
          renderMapElementList();
          mapStatus.textContent = '已选中实例，拖拽或点击空地移动；按 R 旋转';
          return;
        }
        if (selectedPlacementId) {
          const p = placements.find((x) => x.id == selectedPlacementId);
          if (!p) {
            selectedPlacementId = null;
            return;
          }
          const el = elements.find((e) => e.id === p.elementId);
          if (!el) return;
          const aMove = topLeftFromAnchorCell(el, row, col, p.rotation || 0);
          const check = canPlace(el, aMove.row, aMove.col, selectedPlacementId, p.rotation || 0);
          if (!check.ok) {
            showToast('无法移动：' + check.reason, 'err');
            return;
          }
          pushMapHistory();
          p.row = aMove.row;
          p.col = aMove.col;
          selectedPlacementId = null;
          renderMap();
          renderMapElementList();
          showToast('已移动', 'ok');
          return;
        }
        showToast('请选择要放置的元素，或点击地图上的实例', 'err');
        return;
      }

      const element = elements.find((x) => x.id === selectedElementId);
      if (!element) return;
      const aPlace = topLeftFromAnchorCell(element, row, col, pendingRotation);
      const check = canPlace(element, aPlace.row, aPlace.col, null, pendingRotation);
      if (!check.ok) {
        showToast('无法放置：' + check.reason, 'err');
        return;
      }
      pushMapHistory();
      const newPid = allocatePlacementId();
      placements.push({
        id: newPid,
        elementId: element.id,
        row: aPlace.row,
        col: aPlace.col,
        rotation: pendingRotation,
        param: '',
      });
      renderMap();
      if (element.hasParam) {
        openPlacementParamDialog(newPid);
        showToast('已放置「' + element.name + '」，请填写参数', 'ok');
      } else {
        showToast('已放置「' + element.name + '」', 'ok');
      }
    });

    mapCanvas.addEventListener('contextmenu', (e) => {
      const cell = e.target.closest('.cell');
      if (!cell || !mapCanvas.contains(cell)) return;
      e.preventDefault();
      const pid = cell.dataset.placementId;
      if (!pid) return;
      pushMapHistory();
      placements = placements.filter((p) => p.id != pid);
      if (selectedPlacementId == pid) selectedPlacementId = null;
      renderMap();
      renderMapElementList();
      showToast('已删除该元素', 'ok');
    });

    mapCanvas.addEventListener('dragover', (e) => {
      e.preventDefault();
      e.dataTransfer.dropEffect = 'copy';
    });
    mapCanvas.addEventListener('drop', (e) => {
      e.preventDefault();
      const id = e.dataTransfer.getData('application/x-element-id');
      if (!id) return;
      const element = elements.find((x) => x.id === id);
      if (!element) return;
      const rect = mapCanvas.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;
      const col = Math.floor(x / STEP_MAP);
      const row = Math.floor(y / STEP_MAP);
      if (row < 0 || col < 0 || row >= mapH || col >= mapW) return;
      selectedElementId = id;
      selectedPlacementId = null;
      pendingRotation = 0;
      placementPivotRel = null;
      renderMapElementList();
      const aDrop = topLeftFromAnchorCell(element, row, col, pendingRotation);
      const check = canPlace(element, aDrop.row, aDrop.col, null, pendingRotation);
      if (!check.ok) {
        showToast('无法放置：' + check.reason, 'err');
        return;
      }
      pushMapHistory();
      const newPidDrop = allocatePlacementId();
      placements.push({
        id: newPidDrop,
        elementId: element.id,
        row: aDrop.row,
        col: aDrop.col,
        rotation: pendingRotation,
        param: '',
      });
      renderMap();
      if (element.hasParam) {
        openPlacementParamDialog(newPidDrop);
        showToast('已放置「' + element.name + '」，请填写参数', 'ok');
      } else {
        showToast('已放置「' + element.name + '」', 'ok');
      }
    });
  }

  function updateMapHoverLayer() {
    if (!mapCanvasWrap || !mapHoverLayer || !mapCanvasWrap.contains(mapHoverLayer)) return;
    const innerW = mapW * STEP_MAP - GAP;
    const innerH = mapH * STEP_MAP - GAP;
    mapHoverLayer.style.width = innerW + 'px';
    mapHoverLayer.style.height = innerH + 'px';
    mapHoverLayer.style.gridTemplateColumns = `repeat(${mapW}, ${CELL_MAP}px)`;
    mapHoverLayer.style.gridTemplateRows = `repeat(${mapH}, ${CELL_MAP}px)`;
    mapHoverLayer.innerHTML = '';

    if (mapHoverFloatLabel) {
      mapHoverFloatLabel.innerHTML = '';
      mapHoverFloatLabel.hidden = true;
      mapHoverFloatLabel.setAttribute('aria-hidden', 'true');
      mapHoverFloatLabel.style.width = innerW + 'px';
      mapHoverFloatLabel.style.height = innerH + 'px';
    }

    const el = selectedElementId ? elements.find((x) => x.id === selectedElementId) : null;
    if (!el || mapHoverRow < 0 || mapHoverCol < 0) {
      mapHoverLayer.setAttribute('aria-hidden', 'true');
      return;
    }
    mapHoverLayer.setAttribute('aria-hidden', 'false');

    const rc = getCellsWithRotationForElement(el, pendingRotation);
    const anchor = topLeftFromAnchorCell(el, mapHoverRow, mapHoverCol, pendingRotation);
    const check = canPlaceAtHover(el, mapHoverRow, mapHoverCol, null, pendingRotation);
    const valid = check.ok;

    const fpKeySet = new Set();
    rc.forEach(({ r: dr, c: dc }) => fpKeySet.add(`${anchor.row + dr},${anchor.col + dc}`));

    function neighborInHoverFootprint(r, c) {
      if (r < 0 || c < 0 || r >= mapH || c >= mapW) return false;
      return fpKeySet.has(`${r},${c}`);
    }

    const bb = bboxOfCells(rc);
    const bw = bb.w;
    const bh = bb.h;

    for (let r = 0; r < mapH; r++) {
      for (let c = 0; c < mapW; c++) {
        const ghost = document.createElement('div');
        ghost.className = 'ghost';
        const key = `${r},${c}`;
        if (!fpKeySet.has(key)) {
          ghost.classList.add('ghost-empty');
          mapHoverLayer.appendChild(ghost);
          continue;
        }

        const inner = document.createElement('div');
        inner.className = 'ghost-preview-inner' + (valid ? '' : ' ghost-preview-invalid');
        if (showMapAnchors && r === mapHoverRow && c === mapHoverCol) {
          ghost.classList.add('map-anchor-cell');
        }

        if (!isColorOnlyElement(el)) {
          inner.classList.add('has-image');
          const tw = bw * STEP_MAP - GAP;
          const th = bh * STEP_MAP - GAP;
          const dr = r - anchor.row;
          const dc = c - anchor.col;
          inner.style.backgroundImage = 'url(' + el.image + ')';
          inner.style.backgroundSize = tw + 'px ' + th + 'px';
          inner.style.backgroundPosition = -dc * STEP_MAP + 'px ' + -dr * STEP_MAP + 'px';
          inner.style.backgroundRepeat = 'no-repeat';
        } else {
          inner.classList.add('has-color');
          const fillColor = getElementColor(el);
          inner.style.backgroundColor = hexToRgba(fillColor, 0.52);
          const edge = darkenHex(fillColor, 0.4);
          const bwPx = 2;
          if (!neighborInHoverFootprint(r - 1, c)) inner.style.borderTop = bwPx + 'px solid ' + edge;
          if (!neighborInHoverFootprint(r + 1, c)) inner.style.borderBottom = bwPx + 'px solid ' + edge;
          if (!neighborInHoverFootprint(r, c - 1)) inner.style.borderLeft = bwPx + 'px solid ' + edge;
          if (!neighborInHoverFootprint(r, c + 1)) inner.style.borderRight = bwPx + 'px solid ' + edge;
        }

        ghost.appendChild(inner);
        mapHoverLayer.appendChild(ghost);
      }
    }

    if (mapHoverFloatLabel && isColorOnlyElement(el)) {
      const left = anchor.col * STEP_MAP + (bw * STEP_MAP - GAP) / 2;
      const top = anchor.row * STEP_MAP + (bh * STEP_MAP - GAP) / 2;
      const span = document.createElement('div');
      span.className = 'placement-label placement-label-stack hover-preview-label';
      const arr = document.createElement('span');
      arr.className = 'placement-arrow';
      arr.textContent = effectiveArrowChar(el, pendingRotation);
      const nm = document.createElement('span');
      nm.className = 'placement-name';
      nm.textContent = el.name;
      span.appendChild(arr);
      span.appendChild(nm);
      span.style.left = left + 'px';
      span.style.top = top + 'px';
      mapHoverFloatLabel.appendChild(span);
      mapHoverFloatLabel.hidden = false;
      mapHoverFloatLabel.setAttribute('aria-hidden', 'false');
    }
  }

  function clearPlacementDragDimCells() {
    if (!mapCanvas) return;
    mapCanvas.querySelectorAll('.cell.cell-drag-dim').forEach((cell) => cell.classList.remove('cell-drag-dim'));
    mapCanvas.querySelectorAll('.placement-fill.placement-fill-dim').forEach((el) => el.classList.remove('placement-fill-dim'));
    mapCanvas.querySelectorAll('.placement-svg-group.placement-fill-dim').forEach((el) => el.classList.remove('placement-fill-dim'));
  }

  /** 取消选择状态下拖拽移动实例时，在 hover 层显示目标位置预览 */
  function updatePlacementDragPreview(clientX, clientY) {
    if (!mapHoverLayer || !mapCanvas) return;
    const d = mapPlacementDrag;
    if (!d || !d.dragged || !d.pid) return;
    const p = placements.find((x) => x.id == d.pid);
    if (!p) return;
    const el = elements.find((e) => e.id === p.elementId);
    if (!el) return;

    const rect = mapCanvas.getBoundingClientRect();
    const pad = 2;
    const x = clientX - rect.left - pad;
    const y = clientY - rect.top - pad;
    const col = Math.floor(x / STEP_MAP);
    const row = Math.floor(y / STEP_MAP);

    const innerW = mapW * STEP_MAP - GAP;
    const innerH = mapH * STEP_MAP - GAP;
    mapHoverLayer.style.width = innerW + 'px';
    mapHoverLayer.style.height = innerH + 'px';
    mapHoverLayer.style.gridTemplateColumns = `repeat(${mapW}, ${CELL_MAP}px)`;
    mapHoverLayer.style.gridTemplateRows = `repeat(${mapH}, ${CELL_MAP}px)`;
    mapHoverLayer.innerHTML = '';

    if (mapHoverFloatLabel) {
      mapHoverFloatLabel.innerHTML = '';
      mapHoverFloatLabel.hidden = true;
      mapHoverFloatLabel.setAttribute('aria-hidden', 'true');
      mapHoverFloatLabel.style.width = innerW + 'px';
      mapHoverFloatLabel.style.height = innerH + 'px';
    }

    mapCanvas.querySelectorAll('.cell').forEach((cell) => {
      if (cell.dataset.placementId === d.pid) cell.classList.add('cell-drag-dim');
      else cell.classList.remove('cell-drag-dim');
    });
    const fillsInner = mapCanvas.querySelector('.map-placement-fills-inner');
    if (fillsInner) {
      fillsInner.querySelectorAll('.placement-fill').forEach((el) => {
        if (el.dataset.placementId === d.pid) el.classList.add('placement-fill-dim');
        else el.classList.remove('placement-fill-dim');
      });
      fillsInner.querySelectorAll('.placement-svg-group').forEach((el) => {
        if (el.dataset.placementId === d.pid) el.classList.add('placement-fill-dim');
        else el.classList.remove('placement-fill-dim');
      });
    }

    if (row < 0 || col < 0 || row >= mapH || col >= mapW) {
      mapHoverLayer.setAttribute('aria-hidden', 'true');
      return;
    }

    const rot = p.rotation || 0;
    const rc = getCellsWithRotationForElement(el, rot);
    const aMove = topLeftFromAnchorCell(el, row, col, rot);
    const check = canPlace(el, aMove.row, aMove.col, d.pid, rot);
    const valid = check.ok;

    const fpKeySet = new Set();
    rc.forEach(({ r: dr, c: dc }) => fpKeySet.add(`${aMove.row + dr},${aMove.col + dc}`));

    function neighborInDragFootprint(r, c) {
      if (r < 0 || c < 0 || r >= mapH || c >= mapW) return false;
      return fpKeySet.has(`${r},${c}`);
    }

    const bb = bboxOfCells(rc);
    const bw = bb.w;
    const bh = bb.h;

    for (let r = 0; r < mapH; r++) {
      for (let c = 0; c < mapW; c++) {
        const ghost = document.createElement('div');
        ghost.className = 'ghost';
        const key = `${r},${c}`;
        if (!fpKeySet.has(key)) {
          ghost.classList.add('ghost-empty');
          mapHoverLayer.appendChild(ghost);
          continue;
        }

        const inner = document.createElement('div');
        inner.className = 'ghost-preview-inner' + (valid ? '' : ' ghost-preview-invalid');
        const dragAnchorAbs = anchorCellFromTopLeft(el, aMove.row, aMove.col, rot);
        if (showMapAnchors && r === dragAnchorAbs.row && c === dragAnchorAbs.col) {
          ghost.classList.add('map-anchor-cell');
        }

        if (!isColorOnlyElement(el)) {
          inner.classList.add('has-image');
          const tw = bw * STEP_MAP - GAP;
          const th = bh * STEP_MAP - GAP;
          const dr = r - aMove.row;
          const dc = c - aMove.col;
          inner.style.backgroundImage = 'url(' + el.image + ')';
          inner.style.backgroundSize = tw + 'px ' + th + 'px';
          inner.style.backgroundPosition = -dc * STEP_MAP + 'px ' + -dr * STEP_MAP + 'px';
          inner.style.backgroundRepeat = 'no-repeat';
        } else {
          inner.classList.add('has-color');
          const fillColor = getElementColor(el);
          inner.style.backgroundColor = hexToRgba(fillColor, 0.52);
          const edge = darkenHex(fillColor, 0.4);
          const bwPx = 2;
          if (!neighborInDragFootprint(r - 1, c)) inner.style.borderTop = bwPx + 'px solid ' + edge;
          if (!neighborInDragFootprint(r + 1, c)) inner.style.borderBottom = bwPx + 'px solid ' + edge;
          if (!neighborInDragFootprint(r, c - 1)) inner.style.borderLeft = bwPx + 'px solid ' + edge;
          if (!neighborInDragFootprint(r, c + 1)) inner.style.borderRight = bwPx + 'px solid ' + edge;
        }

        ghost.appendChild(inner);
        mapHoverLayer.appendChild(ghost);
      }
    }

    if (mapHoverFloatLabel && isColorOnlyElement(el)) {
      const left = aMove.col * STEP_MAP + (bw * STEP_MAP - GAP) / 2;
      const top = aMove.row * STEP_MAP + (bh * STEP_MAP - GAP) / 2;
      const span = document.createElement('div');
      span.className = 'placement-label placement-label-stack hover-preview-label';
      const arr = document.createElement('span');
      arr.className = 'placement-arrow';
      arr.textContent = effectiveArrowChar(el, rot);
      const nm = document.createElement('span');
      nm.className = 'placement-name';
      nm.textContent = el.name;
      span.appendChild(arr);
      span.appendChild(nm);
      span.style.left = left + 'px';
      span.style.top = top + 'px';
      mapHoverFloatLabel.appendChild(span);
      mapHoverFloatLabel.hidden = false;
      mapHoverFloatLabel.setAttribute('aria-hidden', 'false');
    }

    mapHoverLayer.setAttribute('aria-hidden', 'false');
  }

  function onMapMouseMove(e) {
    if (!mapCanvas) return;
    if (mapPlacementDrag && mapPlacementDrag.dragged) {
      updatePlacementDragPreview(e.clientX, e.clientY);
      return;
    }
    const rect = mapCanvas.getBoundingClientRect();
    const pad = 2;
    const x = e.clientX - rect.left - pad;
    const y = e.clientY - rect.top - pad;
    const col = Math.floor(x / STEP_MAP);
    const row = Math.floor(y / STEP_MAP);
    if (row >= 0 && col >= 0 && row < mapH && col < mapW) {
      if (row !== mapHoverRow || col !== mapHoverCol) {
        mapHoverRow = row;
        mapHoverCol = col;
        updateMapHoverLayer();
      }
    } else {
      if (mapHoverRow >= 0) {
        mapHoverRow = -1;
        mapHoverCol = -1;
        updateMapHoverLayer();
      }
    }
  }

  function onMapMouseLeave() {
    if (mapPlacementDrag && mapPlacementDrag.dragged) return;
    mapHoverRow = -1;
    mapHoverCol = -1;
    updateMapHoverLayer();
  }

  function onMapKeyDown(e) {
    if (isElementEditModalOpen()) return;
    if (isPlacementParamDialogOpen()) return;
    if (isBatchExportDialogOpen()) return;
    if (panelMap.hidden) return;
    const t = e.target;
    if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT')) return;
    if (e.key !== 'r' && e.key !== 'R') return;
    e.preventDefault();
    if (selectedPlacementId) {
      const p = placements.find((x) => x.id == selectedPlacementId);
      if (!p) return;
      const el = elements.find((e) => e.id === p.elementId);
      if (!el) return;
      const oldRot = p.rotation || 0;
      const newRot = ((oldRot + 1) % 4 + 4) % 4;
      const anchorAbs = anchorCellFromTopLeft(el, p.row, p.col, oldRot);
      const nextTopLeft = topLeftFromAnchorCell(el, anchorAbs.row, anchorAbs.col, newRot);
      const check = canPlace(el, nextTopLeft.row, nextTopLeft.col, p.id, newRot);
      if (!check.ok) {
        showToast('无法旋转：' + check.reason, 'err');
        return;
      }
      pushMapHistory();
      p.row = nextTopLeft.row;
      p.col = nextTopLeft.col;
      p.rotation = newRot;
      renderMap();
      showToast('已旋转实例', 'ok');
      return;
    }
    if (selectedElementId) {
      if (mapHoverRow < 0 || mapHoverCol < 0) {
        showToast('请将鼠标移到地图上再按 R 旋转', 'err');
        return;
      }
      const el = elements.find((x) => x.id === selectedElementId);
      if (!el) return;
      pendingRotation = ((pendingRotation + 1) % 4 + 4) % 4;
      placementPivotRel = null;
      updateMapHoverLayer();
      mapStatus.textContent = '放置旋转 ' + pendingRotation + '/4（基于元素锚点）';
    }
  }

  function buildMap(skipHistory) {
    if (!skipHistory) pushMapHistory();
    mapW = Math.max(1, Math.min(128, parseInt(mapGridWInput && mapGridWInput.value, 10) || 14));
    mapH = Math.max(1, Math.min(128, parseInt(mapGridHInput && mapGridHInput.value, 10) || 20));
    if (mapGridWInput) mapGridWInput.value = String(mapW);
    if (mapGridHInput) mapGridHInput.value = String(mapH);
    placements = [];
    nextPlacementId = 0;
    activeMapId = null;
    selectedPlacementId = null;
    pendingRotation = 0;
    placementPivotRel = null;
    renderMap();
    renderMapList();
    mapStatus.textContent = '地图已创建：宽 ' + mapW + ' 列 × 高 ' + mapH + ' 行';
    markMapDirtyBaseline();
    updateUndoRedoButtons();
  }

  // --- Tabs ---
  tabButtons.forEach((btn) => {
    btn.addEventListener('click', () => {
      const tab = btn.dataset.tab;
      tabButtons.forEach((b) => b.classList.toggle('active', b === btn));
      panelElements.classList.toggle('active', tab === 'elements');
      panelElements.hidden = tab !== 'elements';
      panelMap.classList.toggle('active', tab === 'map');
      panelMap.hidden = tab !== 'map';
      if (tab === 'map') {
        setTimeout(() => {
          renderMap();
          requestAnimationFrame(syncMapLibraryMaxHeight);
        }, 0);
      }
    });
  });

  btnCreateElCanvas.addEventListener('click', () => {
    buildElementCanvas();
  });

  elImageInput.addEventListener('change', () => {
    const f = elImageInput.files && elImageInput.files[0];
    if (!f) {
      elImageDataUrl = '';
      elImagePreview.innerHTML = '';
      refreshElementCanvasPreview();
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      elImageDataUrl = reader.result;
      elImagePreview.innerHTML = '<img src="' + elImageDataUrl + '" alt="预览" />';
      refreshElementCanvasPreview();
    };
    reader.readAsDataURL(f);
  });

  if (elColorInput) {
    elColorInput.addEventListener('input', () => refreshElementCanvasPreview());
  }

  if (elName) {
    elName.addEventListener('input', () => refreshLiveElementPreview());
  }

  if (elDirectionInput) {
    elDirectionInput.addEventListener('change', () => refreshLiveElementPreview());
  }

  btnClearSelection.addEventListener('click', () => {
    for (let r = 0; r < elGridN; r++) {
      for (let c = 0; c < elGridN; c++) {
        elSelection[r][c] = false;
      }
    }
    elAnchorCell = null;
    elementCanvas.querySelectorAll('.cell').forEach((cell) => cell.classList.remove('on'));
    repaintCanvasAnchorMark(elementCanvas, elAnchorCell);
    updateElStatus();
    refreshElementCanvasPreview();
  });

  btnSaveElement.addEventListener('click', () => {
    const name = elName.value.trim() || '未命名元素';
    const type = elType.value.trim();
    if (!type) {
      showToast('请填写元素类型', 'err');
      return;
    }
    const cells = normalizeCells(elSelection, elGridN);
    if (cells.length === 0) {
      showToast('请至少选择一格', 'err');
      return;
    }
    if (!elAnchorCell) {
      showToast('请右键一个已选格子设置锚点', 'err');
      return;
    }
    const anchorNorm = normalizeAnchorFromSelection(elSelection, elGridN, elAnchorCell);
    if (!anchorNorm) {
      showToast('锚点无效，请重新设置', 'err');
      return;
    }
    const color = (elColorInput && elColorInput.value) || '#4488cc';
    const direction = elDirectionInput && elDirectionInput.value ? elDirectionInput.value : 'down';
    const elPayload = {
      id: uid(),
      name,
      type,
      gridN: elGridN,
      cells,
      anchor: anchorNorm,
      image: elImageDataUrl || '',
      color: color,
      direction: direction,
      hasParam: !!(elHasParamInput && elHasParamInput.checked),
    };
    const normalized = normalizeElement(elPayload);
    elements.push(normalized);
    persistElements();
    elName.value = '';
    elType.value = '';
    if (elHasParamInput) elHasParamInput.checked = false;
    elImageInput.value = '';
    elImageDataUrl = '';
    elImagePreview.innerHTML = '';
    btnClearSelection.click();
    renderElementList();
    renderMapElementList();
    updateSaveToLibraryButton();
    showToast('已加入元素列表', 'ok');
  });

  if (btnModalCreateElCanvas) {
    btnModalCreateElCanvas.addEventListener('click', () => {
      buildModalElementCanvas();
    });
  }

  if (elModalImageInput) {
    elModalImageInput.addEventListener('change', () => {
      const f = elModalImageInput.files && elModalImageInput.files[0];
      if (!f) {
        modalElImageDataUrl = '';
        if (elModalImagePreview) elModalImagePreview.innerHTML = '';
        refreshModalElementCanvasPreview();
        return;
      }
      const reader = new FileReader();
      reader.onload = () => {
        modalElImageDataUrl = reader.result;
        if (elModalImagePreview) elModalImagePreview.innerHTML = '<img src="' + modalElImageDataUrl + '" alt="预览" />';
        refreshModalElementCanvasPreview();
      };
      reader.readAsDataURL(f);
    });
  }

  if (elModalColorInput) {
    elModalColorInput.addEventListener('input', () => refreshModalElementCanvasPreview());
  }

  if (elModalName) {
    elModalName.addEventListener('input', () => refreshModalLiveElementPreview());
  }

  if (elModalDirection) {
    elModalDirection.addEventListener('change', () => refreshModalLiveElementPreview());
  }

  if (btnModalClearSelection) {
    btnModalClearSelection.addEventListener('click', () => {
      for (let r = 0; r < modalElGridN; r++) {
        for (let c = 0; c < modalElGridN; c++) {
          modalElSelection[r][c] = false;
        }
      }
      modalElAnchorCell = null;
      if (elementModalCanvas) {
        elementModalCanvas.querySelectorAll('.cell').forEach((cell) => cell.classList.remove('on'));
      }
      repaintCanvasAnchorMark(elementModalCanvas, modalElAnchorCell);
      updateModalElStatus();
      refreshModalElementCanvasPreview();
    });
  }

  if (btnModalSaveElement) {
    btnModalSaveElement.addEventListener('click', () => {
      if (!modalEditElementId) return;
      const name = elModalName && elModalName.value.trim() ? elModalName.value.trim() : '未命名元素';
      const type = elModalType && elModalType.value.trim() ? elModalType.value.trim() : '';
      if (!type) {
        showToast('请填写元素类型', 'err');
        return;
      }
      const cells = normalizeCells(modalElSelection, modalElGridN);
      if (cells.length === 0) {
        showToast('请至少选择一格', 'err');
        return;
      }
      if (!modalElAnchorCell) {
        showToast('请右键一个已选格子设置锚点', 'err');
        return;
      }
      const anchorNorm = normalizeAnchorFromSelection(modalElSelection, modalElGridN, modalElAnchorCell);
      if (!anchorNorm) {
        showToast('锚点无效，请重新设置', 'err');
        return;
      }
      const color = (elModalColorInput && elModalColorInput.value) || '#4488cc';
      const direction = elModalDirection && elModalDirection.value ? elModalDirection.value : 'down';
      const elPayload = {
        id: modalEditElementId,
        name,
        type,
        gridN: modalElGridN,
        cells,
        anchor: anchorNorm,
        image: modalElImageDataUrl || '',
        color: color,
        direction: direction,
        hasParam: !!(elModalHasParamInput && elModalHasParamInput.checked),
      };
      const normalized = normalizeElement(elPayload);
      const idx = elements.findIndex((x) => x.id === modalEditElementId);
      if (idx < 0) {
        showToast('找不到要修改的元素', 'err');
        closeElementEditModal();
        return;
      }
      elements[idx] = normalized;
      syncElementIntoSavedMaps(normalized);
      persistElements();
      closeElementEditModal();
      renderElementList();
      renderMapElementList();
      renderMap();
      updateSaveToLibraryButton();
      showToast('已更新元素，并已同步地图列表中的内嵌定义', 'ok');
    });
  }

  if (btnElementEditClose) {
    btnElementEditClose.addEventListener('click', () => {
      closeElementEditModal();
    });
  }

  if (elementEditDialog) {
    const bd = elementEditDialog.querySelector('.modal-backdrop[data-element-edit-dismiss]');
    if (bd) {
      bd.addEventListener('click', () => {
        closeElementEditModal();
      });
    }
  }

  if (placementParamOk) {
    placementParamOk.addEventListener('click', () => {
      if (placementParamEditPid == null) return;
      const p = placements.find((x) => x.id == placementParamEditPid);
      if (!p) {
        closePlacementParamDialog();
        return;
      }
      const next = placementParamInput ? placementParamInput.value : '';
      const prev = typeof p.param === 'string' ? p.param : '';
      if (next !== prev) {
        pushMapHistory();
        p.param = next;
      }
      closePlacementParamDialog();
      renderMap();
      renderMapElementList();
      if (next !== prev) showToast('参数已保存', 'ok');
    });
  }
  if (placementParamCancel) {
    placementParamCancel.addEventListener('click', () => {
      closePlacementParamDialog();
    });
  }
  if (placementParamDialog) {
    const pbd = placementParamDialog.querySelector('.modal-backdrop[data-placement-param-dismiss]');
    if (pbd) {
      pbd.addEventListener('click', () => {
        closePlacementParamDialog();
      });
    }
  }

  if (btnOpenBatchExport) {
    btnOpenBatchExport.addEventListener('click', () => {
      openBatchExportDialog();
    });
  }
  if (batchExportClose) {
    batchExportClose.addEventListener('click', () => {
      closeBatchExportDialog();
    });
  }
  if (batchExportCancel) {
    batchExportCancel.addEventListener('click', () => {
      closeBatchExportDialog();
    });
  }
  if (batchExportDialog) {
    const bbd = batchExportDialog.querySelector('.modal-backdrop[data-batch-export-dismiss]');
    if (bbd) {
      bbd.addEventListener('click', () => {
        closeBatchExportDialog();
      });
    }
  }
  if (batchExportSelectAll) {
    batchExportSelectAll.addEventListener('click', () => {
      batchExportOrderIds.forEach((id) => {
        batchExportChecked[id] = true;
      });
      renderBatchExportList();
    });
  }
  if (batchExportSelectNone) {
    batchExportSelectNone.addEventListener('click', () => {
      batchExportOrderIds.forEach((id) => {
        batchExportChecked[id] = false;
      });
      renderBatchExportList();
    });
  }
  if (batchExportConfirm) {
    batchExportConfirm.addEventListener('click', () => {
      performBatchExport();
    });
  }

  document.addEventListener('keydown', (e) => {
    if (e.key !== 'Escape') return;
    if (isBatchExportDialogOpen()) {
      closeBatchExportDialog();
      e.preventDefault();
      return;
    }
    if (isPlacementParamDialogOpen()) {
      closePlacementParamDialog();
      e.preventDefault();
      return;
    }
    if (!isElementEditModalOpen()) return;
    closeElementEditModal();
    e.preventDefault();
  });

  btnExportElements.addEventListener('click', () => {
    const blob = new Blob([JSON.stringify({ version: VERSION, elements }, null, 2)], { type: 'application/json' });
    downloadBlob(blob, 'elements-library.json');
    showToast('元素库已导出', 'ok');
  });

  importElements.addEventListener('change', () => {
    const f = importElements.files && importElements.files[0];
    if (!f) return;
    const reader = new FileReader();
    reader.onload = () => {
      try {
        const data = JSON.parse(reader.result);
        const list = data.elements || data;
        if (!Array.isArray(list)) throw new Error('格式错误');
        elements = list.filter((x) => x.id && x.cells && Array.isArray(x.cells)).map(normalizeElement);
        selectedElementId = null;
        selectedPlacementId = null;
        pendingRotation = 0;
        placementPivotRel = null;
        closeElementEditModal();
        placements = placements.filter((p) => elements.some((e) => e.id === p.elementId));
        persistElements();
        renderElementList();
        renderMapElementList();
        renderMap();
        showToast('已加载元素库（' + elements.length + ' 个）', 'ok');
      } catch (err) {
        showToast('加载失败：' + err.message, 'err');
      }
    };
    reader.readAsText(f);
    importElements.value = '';
  });

  btnCreateMap.addEventListener('click', () => buildMap());

  if (mapShowAnchorToggle) {
    showMapAnchors = !!mapShowAnchorToggle.checked;
    mapShowAnchorToggle.addEventListener('change', () => {
      showMapAnchors = !!mapShowAnchorToggle.checked;
      persistMapViewPrefs();
      renderMap();
      updateMapHoverLayer();
    });
  }

  if (mapShowPlacementIdToggle) {
    showPlacementIds = !!mapShowPlacementIdToggle.checked;
    mapShowPlacementIdToggle.addEventListener('change', () => {
      showPlacementIds = !!mapShowPlacementIdToggle.checked;
      persistMapViewPrefs();
      renderMap();
    });
  }

  if (mapShowPlacementParamToggle) {
    showPlacementParams = !!mapShowPlacementParamToggle.checked;
    mapShowPlacementParamToggle.addEventListener('change', () => {
      showPlacementParams = !!mapShowPlacementParamToggle.checked;
      persistMapViewPrefs();
      renderMap();
    });
  }

  btnSaveMap.addEventListener('click', () => {
    normalizePlacementIdsBeforeExport();
    const name = mapName.value.trim() || '未命名地图';
    const usedIds = new Set(placements.map((p) => p.elementId));
    const mapElements = elements.filter((e) => usedIds.has(e.id));
    const payload = {
      version: VERSION,
      name,
      width: mapW,
      height: mapH,
      elements: mapElements,
      placements: placements.map((p) => {
        const el = elements.find((e) => e.id === p.elementId);
        const rot = p.rotation || 0;
        return {
          elementId: p.elementId,
          row: p.row,
          col: p.col,
          id: p.id,
          rotation: rot,
          direction: effectivePlacementDirection(el, rot),
          param: typeof p.param === 'string' ? p.param : '',
        };
      }),
    };
    const blob = new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
    downloadBlob(blob, sanitizeFilename(name) + '.json');
    showToast('地图已保存', 'ok');
  });

  if (btnExportGameJson) {
    btnExportGameJson.addEventListener('click', () => {
      normalizePlacementIdsBeforeExport();
      const name = mapName.value.trim() || '未命名地图';
      const payload = {
        kind: GAME_EXPORT_KIND,
        version: GAME_EXPORT_VERSION,
        width: mapW,
        height: mapH,
        instances: placements.map((p) => {
          const el = elements.find((e) => e.id === p.elementId);
          const rot = p.rotation || 0;
          const typeStr = el && el.type != null ? String(el.type).trim() : '';
          const anchor = el ? anchorCellFromTopLeft(el, p.row, p.col, rot) : { row: p.row, col: p.col };
          // 游戏导出坐标系：左下角为 (0,0)，x 向右，y 向上。
          const gameX = anchor.col;
          const gameY = mapH - 1 - anchor.row;
          const inst = {
            id: p.id,
            position: { x: gameX, y: gameY },
            row: gameY,
            col: gameX,
            direction: el ? effectivePlacementDirection(el, rot) : 'down',
            type: typeStr || '默认',
          };
          if (el && el.hasParam) inst.param = typeof p.param === 'string' ? p.param : '';
          return inst;
        }),
      };
      const blob = new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
      downloadBlob(blob, sanitizeFilename(name) + '.game.json');
      showToast('游戏 JSON 已导出', 'ok');
    });
  }

  function sanitizeFilename(s) {
    return s.replace(/[/\\?%*:|"<>]/g, '-').slice(0, 80) || 'map';
  }

  function downloadBlob(blob, filename) {
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = filename;
    a.click();
    setTimeout(() => URL.revokeObjectURL(a.href), 2000);
  }

  importMap.addEventListener('change', () => {
    const f = importMap.files && importMap.files[0];
    if (!f) return;
    const reader = new FileReader();
    reader.onload = () => {
      try {
        const data = JSON.parse(reader.result);
        function runImportFromFile() {
          pushMapHistory();
          activeMapId = null;
          mergeMapIntoEditor(data);
          renderMapList();
          mapStatus.textContent = '已从文件加载';
          showToast('地图已加载', 'ok');
        }
        if (mapHistoryRestoring) {
          runImportFromFile();
        } else if (isMapDirty()) {
          pendingImportPayload = data;
          pendingMapLoadId = null;
          if (mapUnsavedDialog) mapUnsavedDialog.hidden = false;
        } else {
          runImportFromFile();
        }
      } catch (err) {
        showToast('加载失败：' + err.message, 'err');
      }
      importMap.value = '';
    };
    reader.readAsText(f);
  });

  if (btnSaveMapLibrary) {
    btnSaveMapLibrary.addEventListener('click', () => saveMapToLibrary(false));
  }
  if (btnSaveMapAsNew) {
    btnSaveMapAsNew.addEventListener('click', () => saveMapToLibrary(true));
  }

  if (btnMapUndo) btnMapUndo.addEventListener('click', undoMap);
  if (btnMapRedo) btnMapRedo.addEventListener('click', redoMap);
  if (mapName) mapName.addEventListener('input', updateSaveToLibraryButton);

  if (mapUnsavedSave) {
    mapUnsavedSave.addEventListener('click', () => {
      const loadId = pendingMapLoadId;
      const importData = pendingImportPayload;
      saveMapToLibrary(false);
      hideMapUnsavedDialog();
      if (loadId) applyLibraryMapLoad(loadId);
      else if (importData) {
        pushMapHistory();
        activeMapId = null;
        mergeMapIntoEditor(importData);
        renderMapList();
        mapStatus.textContent = '已从文件加载';
        showToast('地图已加载', 'ok');
      }
    });
  }
  if (mapUnsavedDiscard) {
    mapUnsavedDiscard.addEventListener('click', () => {
      const loadId = pendingMapLoadId;
      const importData = pendingImportPayload;
      hideMapUnsavedDialog();
      if (loadId) applyLibraryMapLoad(loadId);
      else if (importData) {
        pushMapHistory();
        activeMapId = null;
        mergeMapIntoEditor(importData);
        renderMapList();
        mapStatus.textContent = '已从文件加载';
        showToast('地图已加载', 'ok');
      }
    });
  }
  if (mapUnsavedCancel) {
    mapUnsavedCancel.addEventListener('click', () => {
      hideMapUnsavedDialog();
      if (importMap) importMap.value = '';
    });
  }
  if (mapUnsavedDialog) {
    const bd = mapUnsavedDialog.querySelector('.modal-backdrop');
    if (bd) {
      bd.addEventListener('click', () => {
        hideMapUnsavedDialog();
        if (importMap) importMap.value = '';
      });
    }
  }

  if (btnExportWorkspace) {
    btnExportWorkspace.addEventListener('click', () => {
      void exportWorkspacePortable();
    });
  }
  if (importWorkspace) {
    importWorkspace.addEventListener('change', () => {
      const f = importWorkspace.files && importWorkspace.files[0];
      if (!f) return;
      const reader = new FileReader();
      reader.onload = () => {
        try {
          const data = JSON.parse(reader.result);
          applyWorkspaceImport(data, { silent: false });
        } catch (err) {
          showToast('加载失败：' + err.message, 'err');
        }
        importWorkspace.value = '';
      };
      reader.readAsText(f);
    });
  }

  // init：若本机尚无 localStorage 数据且通过 http(s) 打开，尝试加载 data/workspace.json
  (async function () {
    await loadWorkspaceExportHandleFromIdb();
    await loadBatchExportDirHandleFromIdb();
    loadElementsFromStorage();
    loadMapLibraryFromStorage();
    loadMapViewPrefs();
    let bundled = false;
    try {
      bundled = await tryLoadBundledWorkspace();
    } catch (e) {
      console.warn('bundled workspace', e);
    }
    buildElementCanvas();
    if (!bundled) {
      buildMap(true);
    }
    ensureMapAxisLayers();
    if (mapCanvasWrap) {
      mapCanvasWrap.addEventListener('mousemove', onMapMouseMove);
      mapCanvasWrap.addEventListener('mouseleave', onMapMouseLeave);
    }
    setupMapInteractions();
    document.addEventListener('keydown', onMapKeyDown);
    renderElementList();
    renderMapElementList();
    renderMapList();
    const sceneEl = document.querySelector('.map-scene-card');
    const libEl = document.querySelector('.map-library-card');
    if (sceneEl && libEl && typeof ResizeObserver !== 'undefined') {
      const ro = new ResizeObserver(() => syncMapLibraryMaxHeight());
      ro.observe(sceneEl);
    }
    window.addEventListener('resize', syncMapLibraryMaxHeight);
    requestAnimationFrame(syncMapLibraryMaxHeight);
  })();
})();
