import os
from PIL import Image, ImageDraw

# Create output directory
output_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'VoiceInput', 'Resources')
os.makedirs(output_dir, exist_ok=True)

def create_microphone_icon(size):
    """Create a microphone icon using PIL"""
    # Create a new image with dark background
    img = Image.new('RGBA', (size, size), (26, 26, 26, 255))  # #1a1a1a
    draw = ImageDraw.Draw(img)
    
    # Calculate scaling factor
    scale = size / 24
    
    # Define colors
    white = (255, 255, 255, 255)
    
    # Draw rounded rectangle background
    corner_radius = int(4 * scale)
    draw.rounded_rectangle([(0, 0), (size-1, size-1)], radius=corner_radius, fill=(26, 26, 26, 255))
    
    # Draw microphone body (capsule shape)
    mic_x = int(12 * scale)
    mic_y_top = int(2 * scale)
    mic_width = int(6 * scale)
    mic_height = int(10 * scale)
    
    # Microphone capsule
    mic_left = mic_x - mic_width // 2
    mic_right = mic_x + mic_width // 2
    mic_bottom = mic_y_top + mic_height
    
    # Draw microphone body
    draw.rounded_rectangle(
        [(mic_left, mic_y_top), (mic_right, mic_bottom)],
        radius=mic_width // 2,
        outline=white,
        width=max(1, int(2 * scale))
    )
    
    # Draw microphone arc (stand)
    arc_top = int(10 * scale)
    arc_bottom = int(17 * scale)
    arc_left = int(5 * scale)
    arc_right = int(19 * scale)
    
    # Draw the arc
    draw.arc(
        [(arc_left, arc_top), (arc_right, arc_bottom)],
        start=0,
        end=180,
        fill=white,
        width=max(1, int(2 * scale))
    )
    
    # Draw microphone stem
    stem_top = int(19 * scale)
    stem_bottom = int(22 * scale)
    draw.line(
        [(mic_x, stem_top), (mic_x, stem_bottom)],
        fill=white,
        width=max(1, int(2 * scale))
    )
    
    return img

# Generate PNG files for different sizes
sizes = [16, 32, 48, 64, 128, 256]

for size in sizes:
    img = create_microphone_icon(size)
    img.save(os.path.join(output_dir, f'icon_{size}.png'))
    print(f"Generated icon_{size}.png")

# Generate main icon.png (256x256)
img = create_microphone_icon(256)
img.save(os.path.join(output_dir, 'icon.png'))
print("Generated icon.png (256x256)")

# Generate ICO file with multiple sizes
images = []
for size in [16, 32, 48, 256]:
    img = create_microphone_icon(size)
    images.append(img)

# Save as ICO
images[-1].save(
    os.path.join(output_dir, 'app.ico'), 
    format='ICO', 
    sizes=[(16, 16), (32, 32), (48, 48), (256, 256)]
)
print("Generated app.ico")

print(f"\nAll icons generated in: {output_dir}")