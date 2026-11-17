import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const project = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const record = JSON.parse(fs.readFileSync(path.join(project, 'Docs/Release/LastBuild.json'), 'utf8'));
const root = path.resolve(project, process.argv[2] || record.output);
const port = Number(process.argv[3] || 8765);
const types = { '.html': 'text/html; charset=utf-8', '.js': 'application/javascript', '.wasm': 'application/wasm', '.json': 'application/json', '.css': 'text/css', '.png': 'image/png', '.txt': 'text/plain; charset=utf-8', '.md': 'text/plain; charset=utf-8' };
const server = http.createServer((request, response) => {
  let pathname;
  try { pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname); }
  catch { response.writeHead(400).end(); return; }
  if (pathname === '/preview') {
    response.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    response.end('<!doctype html><html lang="en"><meta name="viewport" content="width=device-width,initial-scale=1"><title>TossRush embed preview</title><style>body{margin:0;background:#182331;color:#fff;font:16px system-ui}main{max-width:1280px;margin:24px auto}iframe{width:100%;aspect-ratio:16/9;border:0;display:block}h1,p{margin:16px}</style><main><h1>TossRush — local embed preview</h1><iframe title="TossRush game" src="/" allow="autoplay; fullscreen" allowfullscreen></iframe><p>Mouse: aim · Left click: throw · Esc: release mouse · R: replay after results</p></main></html>');
    return;
  }
  const file = path.resolve(root, '.' + (pathname === '/' ? '/index.html' : pathname));
  if (!file.startsWith(root + path.sep)) { response.writeHead(403).end(); return; }
  if (!fs.existsSync(file) || !fs.statSync(file).isFile()) { response.writeHead(404).end(); return; }
  response.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
  fs.createReadStream(file).pipe(response);
});
server.listen(port, '127.0.0.1', () => console.log(`TossRush preview: http://127.0.0.1:${port}/preview\nServing ${root}`));
