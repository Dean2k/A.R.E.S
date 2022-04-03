import hashlib, os

base = os.getcwd()
os.chdir('..')
with open(f"ARES_C\\ARES\\ARES\\bin\\Release\\ARES.exe", "rb") as f:
    ARESDATA = f.read()
    GUIHash = hashlib.sha256(ARESDATA).hexdigest()
    print('GUI Hash: ' + GUIHash)
    with open(f'{base}\\ARESGUI.txt', 'w+')as f:
        f.write(GUIHash)