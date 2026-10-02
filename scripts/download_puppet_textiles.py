"""Fetch B2-only CC0 scan maps from recorded primary API URLs; verify each digest."""
import hashlib,json,urllib.request
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'assets/b2_puppet_textures'
maps=json.loads((root/'source-metadata.json').read_text(encoding='utf-8-sig'))
records=[]
for asset in ('caban','wool_boucle'):
    for kind in ('Displacement','nor_gl'):
        item=maps[asset][kind]['1k']['jpg']
        path=root/item['url'].rsplit('/',1)[-1]
        with urllib.request.urlopen(item['url'],timeout=45) as response:
            data=response.read()
        assert hashlib.md5(data).hexdigest()==item['md5'],str(path)
        path.write_bytes(data)
        records.append(dict(asset=asset,kind=kind,source=item['url'],file=path.name,md5=item['md5'],sha256=hashlib.sha256(data).hexdigest()))
        print(path.name,len(data))
(root/'downloads.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
