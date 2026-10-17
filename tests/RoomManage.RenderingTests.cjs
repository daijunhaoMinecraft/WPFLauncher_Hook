// 先运行 npm install --prefix .artifacts/room-render-probe jsdom@24.1.3 --ignore-scripts --no-audit --no-fund。
// PowerShell: $env:NODE_PATH = (Resolve-Path '.artifacts/room-render-probe/node_modules').Path; node tests/RoomManage.RenderingTests.cjs
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('jsdom');

const htmlPath = path.resolve(__dirname, '../Mcl.Core/Dotnetdetour/UI/WebAssets/RoomManage.html');
const html = fs.readFileSync(htmlPath, 'utf8');
const scripts = [...html.matchAll(/<script>([\s\S]*?)<\/script>/g)];
if (scripts.length !== 1) throw new Error(`Expected one inline script, found ${scripts.length}`);

const dom = new JSDOM(`<!doctype html><body>
    <div id="blacklist-container"></div><div id="no-blacklist-message"></div>
    <div id="players-container"></div><div id="no-players-message"></div>
    <div id="regex-blacklist-container"></div><div id="no-regex-message"></div>
    </body>`, { runScripts: 'outside-only', url: 'http://localhost/' });
const { window } = dom;
const { document } = window;
const addEventListener = window.addEventListener.bind(window);
window.addEventListener = (type, ...args) => {
    if (type !== 'load') addEventListener(type, ...args); // 不执行页面初始化、网络或 WebSocket。
};
window.fetch = () => { throw new Error('Network calls are disabled in render tests'); };
window.WebSocket = class { constructor() { throw new Error('WebSocket is disabled in render tests'); } };
window.eval(scripts[0][1] + '\nwindow.__setCardState = function(nextPlayers, nextRegex, owner) { players = nextPlayers; regexBlacklist = nextRegex; isOwner = owner; };');

let failures = 0;
function check(value, name) {
    if (value) return;
    console.error(`FAIL: ${name}`);
    failures++;
}
const text = value => value?.textContent;
const markup = `<span data-injected="1">BAD</span>&"'`;
const rawId = `id" data-injected="1&'<tag>`;
const avatarUrl = `https://example.invalid/avatar.png" data-injected="1`;
const pattern = `(?<name>foo)&"'<span data-injected="1">BAD</span>`;
const calls = [];
window.removeFromBlacklist = value => calls.push(['remove', value]);
window.kickPlayer = value => calls.push(['kick', value]);
window.addToBlacklist = value => calls.push(['add', value]);
window.removeFromRegexBlacklist = value => calls.push(['regex', value]);

window.renderBlacklistGrid([{ userId: rawId, userName: markup, signature: markup, avatarUrl }]);
const blacklist = document.getElementById('blacklist-container');
check(blacklist.children.length === 1, 'blacklist card count');
check(text(blacklist.querySelector('.player-name')) === markup, 'blacklist name text');
check(text(blacklist.querySelector('.player-signature')) === markup, 'blacklist signature text');
check(text(blacklist.querySelector('.player-id')) === `ID: ${rawId}`, 'blacklist ID text');
check(blacklist.querySelector('img')?.getAttribute('src') === avatarUrl, 'blacklist avatar URL attribute');
check(blacklist.querySelector('img')?.getAttribute('alt') === markup, 'blacklist avatar alt attribute');
check(blacklist.querySelector('.remove-blacklist-btn')?.getAttribute('data-user-id') === rawId, 'blacklist action raw ID');
check(!blacklist.querySelector('[data-injected]'), 'blacklist has no injected nodes or attributes');
blacklist.querySelector('.remove-blacklist-btn')?.click();

window.__setCardState([
    { userId: rawId, playerName: markup, signature: markup, avatarUrl, role: markup, ident: 0 },
    { userId: 'owner-id', playerName: '房主', role: '房主', ident: 1 }
], [], true);
window.renderPlayersGrid();
const players = document.getElementById('players-container');
check(players.children.length === 2, 'player card count');
const member = players.children[0];
const owner = players.children[1];
check(text(member?.querySelector('.player-name')) === markup, 'player name text');
check(text(member?.querySelector('.player-signature')) === markup, 'player signature text');
check(text(member?.querySelector('.player-role')) === markup, 'player role text');
check(text(member?.querySelector('.player-id')) === `ID: ${rawId}`, 'player ID text');
check(member?.querySelector('img')?.getAttribute('src') === avatarUrl, 'player avatar URL attribute');
check(member?.querySelector('img')?.getAttribute('alt') === markup, 'player avatar alt attribute');
check(member?.querySelector('.kick-player-btn')?.getAttribute('data-user-id') === rawId, 'kick action raw ID');
check(member?.querySelector('.add-blacklist-btn')?.getAttribute('data-user-id') === rawId, 'add action raw ID');
check(!players.querySelector('[data-injected]'), 'players have no injected nodes or attributes');
check(owner?.querySelector('.player-role')?.classList.contains('owner'), 'owner style retained');
check(!owner?.querySelector('.actions'), 'owner has no member actions');
member?.querySelector('.kick-player-btn')?.click();
member?.querySelector('.add-blacklist-btn')?.click();

const regex = document.getElementById('regex-blacklist-container');
window.__setCardState([], ['(?<name>foo)'], true);
window.renderRegexBlacklistGrid();
check(text(regex.querySelector('.player-id')) === '(?<name>foo)', 'plain named group stays visible');

window.__setCardState([], [pattern], true);
window.renderRegexBlacklistGrid();
check(regex.children.length === 1, 'regex card count');
check(text(regex.querySelector('.player-id')) === pattern, 'named group and punctuation stay visible');
check(regex.querySelector('.remove-regex-btn')?.getAttribute('data-pattern') === pattern, 'regex action raw pattern');
check(!regex.querySelector('[data-injected]'), 'regex has no injected nodes or attributes');
regex.querySelector('.remove-regex-btn')?.click();
check(JSON.stringify(calls) === JSON.stringify([
    ['remove', rawId], ['kick', rawId], ['add', rawId], ['regex', pattern]
]), 'buttons pass unencoded values to handlers');

window.renderBlacklistGrid([]);
window.__setCardState([], [], true);
window.renderPlayersGrid();
window.renderRegexBlacklistGrid();
for (const [containerId, emptyId] of [
    ['blacklist-container', 'no-blacklist-message'],
    ['players-container', 'no-players-message'],
    ['regex-blacklist-container', 'no-regex-message']
]) {
    check(document.getElementById(containerId).children.length === 0, `${containerId} empty contents`);
    check(document.getElementById(emptyId).style.display === 'block', `${emptyId} visible`);
}

console.log(`Room card rendering: ${failures === 0 ? 'PASS' : 'FAIL'}, ${failures} failed checks`);
dom.window.close();
if (failures) process.exitCode = 1;
