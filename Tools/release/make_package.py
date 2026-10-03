import os, shutil, zipfile

# Run from anywhere:  python Tools/release/make_package.py   (after building Core, Loader, Tools/gametool, Tools/bootstrap in Release)
HERE = os.path.dirname(os.path.abspath(__file__))
B = os.path.abspath(os.path.join(HERE, '..', '..'))
core = os.path.join(B, 'Subnautica.Core', 'bin', 'Release', 'net472')
loader = os.path.join(B, 'Subnautica.Loader', 'bin', 'Release', 'net472', 'Subnautica.Loader.dll')
spawn = os.path.join(B, 'Data', 'SpawnPoints.bin')
gt = os.path.join(B, 'Tools', 'gametool', 'bin', 'Release', 'net472')
bs = os.path.join(B, 'Tools', 'bootstrap', 'bin', 'Release', 'net472', 'SubnauticaBootstrap.dll')
launcher = os.path.join(B, 'Tools', 'launcher', 'bin', 'Release', 'net472', 'SubnauticaBZ-Launcher.exe')
src = HERE

pkg = os.path.join(B, 'out', 'pkg', 'SubnauticaBZ-Multiplayer-LAN')
if os.path.exists(os.path.join(B, 'out', 'pkg')):
    shutil.rmtree(os.path.join(B, 'out', 'pkg'))
game = os.path.join(pkg, 'Multiplayer', 'Game')
for d in ['Core', 'Dependencies', 'Logs', 'Plugins', 'Saves']:
    os.makedirs(os.path.join(game, d))
os.makedirs(os.path.join(pkg, 'Patcher'))

shutil.copy(loader, game)
shutil.copy(spawn, os.path.join(game, 'Core'))
deps = [f for f in os.listdir(core) if f.lower().endswith('.dll')]
for f in deps:
    shutil.copy(os.path.join(core, f), os.path.join(game, 'Dependencies'))
for f in ['gametool.exe', 'gametool.exe.config', 'Mono.Cecil.dll', 'Mono.Cecil.Mdb.dll', 'Mono.Cecil.Pdb.dll', 'Mono.Cecil.Rocks.dll']:
    shutil.copy(os.path.join(gt, f), os.path.join(pkg, 'Patcher'))
shutil.copy(bs, os.path.join(pkg, 'Patcher'))
shutil.copy(launcher, pkg)

for f in ['Install.bat', 'Uninstall.bat', 'README-LAN.txt']:
    data = open(os.path.join(src, f), 'rb').read().decode('utf-8').replace('\r\n', '\n').replace('\n', '\r\n')
    open(os.path.join(pkg, f), 'wb').write(data.encode('utf-8'))

zpath = os.path.join(B, 'out', 'SubnauticaBZ-Multiplayer-LAN.zip')
if os.path.exists(zpath):
    os.remove(zpath)
with zipfile.ZipFile(zpath, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for root, dirs, files in os.walk(pkg):
        rel_root = os.path.relpath(root, os.path.dirname(pkg))
        if not files and not dirs:
            z.write(root, rel_root.replace(os.sep, '/') + '/')
        for f in files:
            full = os.path.join(root, f)
            z.write(full, os.path.relpath(full, os.path.dirname(pkg)).replace(os.sep, '/'))

print('dependencies:', sorted(deps))
print('zip:', zpath, round(os.path.getsize(zpath) / 1e6, 1), 'MB')
with zipfile.ZipFile(zpath) as z:
    for n in z.namelist():
        print(' ', n)
