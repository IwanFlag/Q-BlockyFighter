import codecs, re

path = r'D:\software\Q版本流星蝴蝶剑\Code\index_local_v03_enhanced.html'

# Read as binary
with open(path, 'rb') as f:
    raw = f.read()

# Remove BOM if present
if raw[:3] == b'\xef\xbb\xbf':
    print('Removing BOM...')
    raw = raw[3:]

# Write back as clean UTF-8 without BOM
with open(path, 'wb') as f:
    f.write(raw)

# Verify
with open(path, 'rb') as f:
    first = f.read(10)
    print('First bytes:', first.hex())
    print('Has BOM:', first[:3] == b'\xef\xbb\xbf')

# Check charset meta tag
with open(path, 'r', encoding='utf-8') as f:
    head = f.read(3000)

match = re.search(r'charset\s*=\s*["\']?([^"\';\s>]+)', head)
if match:
    print(f'Charset: {match.group(1)}')
else:
    print('No charset found!')

print(f'File size: {len(raw)} bytes')
