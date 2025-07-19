import os
from PIL import Image, ImageDraw, ImageFont
import cairosvg
import io

# SVG content
svg_content = '''<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
  <rect x="0" y="0" width="24" height="24" fill="#1a1a1a" rx="4"/>
  <g transform="translate(0, 0)">
    <path d="M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3Z"/>
    <path d="M19 10v2a7 7 0 0 1-14 0v-2"/>
    <line x1="12" x2="12" y1="19" y2="22"/>
  </g>
</svg>'''

# Create output directory
output_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'VoiceInput', 'Resources')
os.makedirs(output_dir, exist_ok=True)

# Generate PNG files for different sizes
sizes = [16, 32, 48, 64, 128, 256]

for size in sizes:
    # Modify SVG with appropriate size
    sized_svg = svg_content.replace('width="256"', f'width="{size}"').replace('height="256"', f'height="{size}"')
    
    # Convert SVG to PNG
    png_data = cairosvg.svg2png(bytestring=sized_svg.encode('utf-8'), output_width=size, output_height=size)
    
    # Save PNG
    with open(os.path.join(output_dir, f'icon_{size}.png'), 'wb') as f:
        f.write(png_data)
    
    print(f"Generated icon_{size}.png")

# Generate main icon.png (256x256)
main_svg = svg_content
png_data = cairosvg.svg2png(bytestring=main_svg.encode('utf-8'), output_width=256, output_height=256)
with open(os.path.join(output_dir, 'icon.png'), 'wb') as f:
    f.write(png_data)
print("Generated icon.png (256x256)")

# Generate ICO file with multiple sizes
images = []
for size in [16, 32, 48, 64, 128, 256]:
    img = Image.open(os.path.join(output_dir, f'icon_{size}.png'))
    images.append(img)

# Save as ICO
images[5].save(os.path.join(output_dir, 'app.ico'), format='ICO', sizes=[(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
print("Generated app.ico")

print(f"\nAll icons generated in: {output_dir}")