// scripts/create-scooter-glb.js
// Generates a simple low-poly scooter and writes it as a single-file GLB (glTF 2.0 binary)
// Usage: node scripts/create-scooter-glb.js

const fs = require('fs');

// Helpers
function floatToBytes(floatArray) {
  const buf = Buffer.alloc(floatArray.length * 4);
  for (let i = 0; i < floatArray.length; i++) buf.writeFloatLE(floatArray[i], i * 4);
  return buf;
}
function uint16ToBytes(uint16Array) {
  const buf = Buffer.alloc(uint16Array.length * 2);
  for (let i = 0; i < uint16Array.length; i++) buf.writeUInt16LE(uint16Array[i], i * 2);
  return buf;
}

// Geometry builder
let positions = []; // Float32 list
let normals = [];   // Float32 list
let indexChunks = []; // Array of {indices:[], indexComponentType, count}
let vertexCount = 0;

function addPrimitive(verts, tris) {
  // verts: array of [x,y,z]
  // tris: array of index triples referring to verts
  for (const v of verts) {
    positions.push(v[0], v[1], v[2]);
    // simple flat normal facing +Z
    normals.push(0, 0, 1);
  }
  const indices = tris.map(i => i);
  // Offset indices by current vertexCount
  for (let i = 0; i < indices.length; i++) indices[i] += vertexCount;
  indexChunks.push({ indices, count: indices.length });
  vertexCount += verts.length;
}

// Define simple scooter geometry (flat, in X/Y plane, z=0)
// Body rectangle
addPrimitive(
  [
    [-0.6, 0.05, 0], // 0
    [ 0.4, 0.05, 0], // 1
    [ 0.4, 0.25, 0], // 2
    [-0.6, 0.25, 0], // 3
  ],
  [0,1,2, 0,2,3]
);
// Handle rectangle
addPrimitive(
  [
    [0.35, 0.25, 0], // 4
    [0.5, 0.25, 0],  // 5
    [0.5, 0.35, 0],  // 6
    [0.35,0.35,0],   // 7
  ],
  [0,1,2, 0,2,3]
);

// Wheel function - add a simple 8-gon fan
function addWheel(cx, cy, r, segments = 8) {
  const baseIndex = vertexCount;
  const verts = [];
  verts.push([cx, cy, 0]); // center
  for (let i = 0; i < segments; i++) {
    const a = (i / segments) * Math.PI * 2;
    verts.push([cx + Math.cos(a) * r, cy + Math.sin(a) * r, 0]);
  }
  // triangles: fan from center (0), triangles [0, i, i+1]
  const tris = [];
  for (let i = 1; i <= segments; i++) {
    const a = i;
    const b = (i % segments) + 1;
    tris.push(0, a, b);
  }
  addPrimitive(verts, tris);
}

addWheel(-0.4, 0, 0.12);
addWheel(0.25, 0, 0.12);

// Build binary buffers
const posBuf = floatToBytes(positions);
const norBuf = floatToBytes(normals);
// Concatenate all index chunks into a single index buffer but record bufferViews/accessors per primitive
let indicesBufs = indexChunks.map(chunk => uint16ToBytes(chunk.indices));
const totalIndexLength = indicesBufs.reduce((s,b)=>s+b.length,0);
const indexBuf = Buffer.concat(indicesBufs);

// We'll interleave buffers sequentially: positions, normals, indices(all)
// Compute offsets
let offset = 0;
const posOffset = offset; offset += posBuf.length;
const norOffset = offset; offset += norBuf.length;
// For indices, we'll create a bufferView per chunk, so need offsets per chunk
let indexOffsets = [];
let idxOff = offset;
for (const b of indicesBufs) {
  indexOffsets.push(idxOff);
  idxOff += b.length;
}
const binBuffer = Buffer.concat([posBuf, norBuf, indexBuf]);

// glTF JSON
const gltf = {
  asset: { generator: "simple-scooter-generator", version: "2.0" },
  scenes: [{ nodes: [0] }],
  nodes: [{ mesh: 0 }],
  meshes: [{ name: "scooter", primitives: [] }],
  buffers: [{ byteLength: binBuffer.length }],
  bufferViews: [],
  accessors: [],
  materials: [
    { name: "body", pbrMetallicRoughness: { baseColorFactor: [0.0, 0.6, 0.4, 1.0], metallicFactor: 0.0, roughnessFactor: 0.9 } },
    { name: "wheel", pbrMetallicRoughness: { baseColorFactor: [0.02, 0.02, 0.02, 1.0], metallicFactor: 0.0, roughnessFactor: 0.9 } }
  ]
};

// add bufferViews & accessors
// positions
gltf.bufferViews.push({ buffer: 0, byteOffset: posOffset, byteLength: posBuf.length });
const posAccessorIndex = gltf.accessors.length;
gltf.accessors.push({ bufferView: gltf.bufferViews.length - 1, byteOffset: 0, componentType: 5126, count: positions.length / 3, type: "VEC3", min: [ -0.6, 0, 0 ], max: [ 0.5, 0.35, 0 ] });
// normals
gltf.bufferViews.push({ buffer: 0, byteOffset: norOffset, byteLength: norBuf.length });
const norAccessorIndex = gltf.accessors.length;
gltf.accessors.push({ bufferView: gltf.bufferViews.length - 1, byteOffset: 0, componentType: 5126, count: normals.length / 3, type: "VEC3" });

// indices: create a bufferView+accessor for each index chunk and a primitive referencing it
let runningIndexBase = 0; // not used directly
let primitiveIndex = 0;
for (let i = 0; i < indexChunks.length; i++) {
  const ivOffset = indexOffsets[i];
  const ivLength = indicesBufs[i].length;
  gltf.bufferViews.push({ buffer: 0, byteOffset: ivOffset, byteLength: ivLength, target: 34963 }); // ELEMENT_ARRAY_BUFFER
  const accessorIndex = gltf.accessors.length;
  gltf.accessors.push({ bufferView: gltf.bufferViews.length - 1, byteOffset: 0, componentType: 5123, count: indexChunks[i].count, type: "SCALAR" });

  // Each primitive uses a contiguous range of the overall positions/normals accessors.
  // We need to know which vertex range this primitive used. We know vertices were appended in sequence.
  // We'll create a simple primitive referencing the corresponding subset by creating smaller accessors pointing to the portion of the position/normal buffer.

  // Determine start vertex for this primitive by summing previous verts
  let vertStart = 0;
  for (let j = 0; j < i; j++) {
    // sum number of vertices of earlier primitives: each primitive's vertex count is (indexChunks[j] ? compute from indices?)
  }
  // Simpler approach: we'll create unique position/normal bufferViews/accessors for each primitive slice instead.
  // Extract the vertex count for this primitive by examining indices and finding max index used relative to its local range.
  // However we built verts sequentially and recorded indexChunks with indices already offset by vertexCount at time of addition.
  // To get the number of vertices for primitive i, find all unique vertex indices that appear in indexChunks[i] and compute range.
  const inds = indexChunks[i].indices;
  const minIdx = Math.min(...inds);
  const maxIdx = Math.max(...inds);
  const vertexCountForPrim = (maxIdx - minIdx) + 1;
  const vertexByteOffset = minIdx * 12; // 3 components * 4 bytes
  const normalsByteOffset = minIdx * 12;

  // Create bufferView+accessor for this primitive's positions slice
  gltf.bufferViews.push({ buffer: 0, byteOffset: posOffset + vertexByteOffset, byteLength: vertexCountForPrim * 12 });
  const primPosAccessor = gltf.accessors.length;
  gltf.accessors.push({ bufferView: gltf.bufferViews.length - 1, byteOffset: 0, componentType: 5126, count: vertexCountForPrim, type: "VEC3" });

  // Normals slice
  gltf.bufferViews.push({ buffer: 0, byteOffset: norOffset + normalsByteOffset, byteLength: vertexCountForPrim * 12 });
  const primNorAccessor = gltf.accessors.length;
  gltf.accessors.push({ bufferView: gltf.bufferViews.length - 1, byteOffset: 0, componentType: 5126, count: vertexCountForPrim, type: "VEC3" });

  // Create primitive
  const materialIndex = (primitiveIndex < 2) ? 0 : 1; // first two primitives = body material, remaining = wheel material
  const prim = {
    attributes: { POSITION: primPosAccessor, NORMAL: primNorAccessor },
    indices: accessorIndex,
    material: materialIndex
  };
  gltf.meshes[0].primitives.push(prim);
  primitiveIndex++;
}

// Write GLB: header + JSON chunk + BIN chunk
function padBuffer(buf) {
  const pad = (4 - (buf.length % 4)) % 4;
  if (pad === 0) return buf;
  return Buffer.concat([buf, Buffer.alloc(pad)]);
}

const jsonChunk = Buffer.from(JSON.stringify(gltf));
const jsonChunkPadded = padBuffer(jsonChunk);
const binChunkPadded = padBuffer(binBuffer);

const jsonChunkHeader = Buffer.alloc(8);
jsonChunkHeader.writeUInt32LE(jsonChunkPadded.length, 0);
jsonChunkHeader.writeUInt32LE(0x4E4F534A, 4); // 'JSON'
const binChunkHeader = Buffer.alloc(8);
binChunkHeader.writeUInt32LE(binChunkPadded.length, 0);
binChunkHeader.writeUInt32LE(0x004E4942, 4); // 'BIN\0'

const header = Buffer.alloc(12);
header.writeUInt32LE(0x46546C67, 0); // magic 'glTF'
header.writeUInt32LE(2, 4); // version
const totalLength = 12 + 8 + jsonChunkPadded.length + 8 + binChunkPadded.length;
header.writeUInt32LE(totalLength, 8);

const out = Buffer.concat([header, jsonChunkHeader, jsonChunkPadded, binChunkHeader, binChunkPadded]);
fs.mkdirSync('assets/scooter', { recursive: true });
fs.writeFileSync('assets/scooter/scooter.glb', out);
console.log('Written assets/scooter/scooter.glb (' + out.length + ' bytes)');
console.log('Done. You can view the file in a glTF viewer (e.g., https://gltf-viewer.donmccurdy.com/).');
