// Native Unity delivery: keep the Data directory and engine DLLs beside the one game EXE.
const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const { runRelease } = require('C:/work/project-resume/tools/game-release.cjs');

const project = path.resolve(__dirname, '..');
const projectId = 'SquadVsMonster';
const settings = fs.readFileSync(path.join(project, 'ProjectSettings/ProjectSettings.asset'), 'utf8');
const version = settings.match(/^  bundleVersion: (.+)$/m)?.[1].trim();
if (!version) throw new Error('Unity bundleVersion missing');
const editorVersion = fs.readFileSync(path.join(project, 'ProjectSettings/ProjectVersion.txt'), 'utf8')
  .match(/^m_EditorVersion: (.+)$/m)?.[1].trim();
const unity = process.env.UNITY_EDITOR || `C:/Program Files/Unity/Hub/Editor/${editorVersion}/Editor/Unity.exe`;
if (!fs.existsSync(unity)) throw new Error(`Unity Editor missing: ${unity}`);

runRelease({
  projectId,
  version,
  preview: true,
  build: async (stage) => {
    const payload = path.join(stage, `unity-${Date.now()}`);
    fs.mkdirSync(payload, { recursive: true });
    const gameExe = path.join(payload, `${projectId}.exe`);
    const log = path.join(stage, 'unity-build.log');
    const result = spawnSync(unity, ['-batchmode', '-quit', '-projectPath', project,
      '-buildWindows64Player', gameExe, '-logFile', log], { stdio: 'inherit' });
    if (result.error) throw result.error;
    if (result.status !== 0) throw new Error(`Unity build failed (${result.status}); see ${log}`);
    if (!fs.existsSync(gameExe) || !fs.existsSync(path.join(payload, `${projectId}_Data`)) ||
        !fs.existsSync(path.join(payload, 'UnityPlayer.dll'))) {
      throw new Error(`Unity build incomplete; see ${log}`);
    }
    const delivery = path.join('D:/CodexOutputs', projectId);
    // Engine files are required at runtime; this is not a self-contained portable wrapper.
    for (const entry of fs.readdirSync(payload)) {
      if (entry === `${projectId}.exe`) continue;
      fs.cpSync(path.join(payload, entry), path.join(delivery, entry), { recursive: true });
    }
    const stagedExe = path.join(stage, `${projectId}.exe`);
    fs.copyFileSync(gameExe, stagedExe);
    return [stagedExe];
  },
}).catch((error) => { console.error(error.message); process.exitCode = 1; });
