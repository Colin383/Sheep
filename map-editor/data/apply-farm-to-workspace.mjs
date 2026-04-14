import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const mapPath = path.join(__dirname, 'farm-puzzle-level.map.json');
const map = JSON.parse(fs.readFileSync(mapPath, 'utf8'));

const workspace = {
  version: 4,
  kind: 'map-editor-workspace',
  elements: map.elements,
  maps: [
    {
      id: 'farm-puzzle-ref-001',
      name: map.name,
      width: map.width,
      height: map.height,
      elements: map.elements,
      placements: map.placements.map((p) => ({
        id: p.id,
        elementId: p.elementId,
        row: p.row,
        col: p.col,
        rotation: p.rotation ?? 0,
        direction: p.direction || 'down',
        param: typeof p.param === 'string' ? p.param : '',
      })),
      updatedAt: Date.now(),
    },
  ],
};

const out = path.join(__dirname, 'workspace.json');
fs.writeFileSync(out, JSON.stringify(workspace, null, 2), 'utf8');
console.log('Updated', out);
