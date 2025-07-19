import os
from PIL import Image, ImageDraw, ImageOps

# Create output directory
output_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'VoiceInput', 'Resources')
os.makedirs(output_dir, exist_ok=True)

def create_rounded_rectangle_mask(size, radius):
    """Create a mask for rounded rectangle"""
    mask = Image.new('L', (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle([(0, 0), (size-1, size-1)], radius=radius, fill=255)
    return mask

def create_high_quality_microphone_icon(size):
    """Create a high-quality microphone icon with anti-aliasing"""
    # Create at 4x size for better quality, then downscale
    work_size = size * 4
    img = Image.new('RGBA', (work_size, work_size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Calculate scaling factor
    scale = work_size / 24
    
    # Define colors
    bg_color = (26, 26, 26, 255)  # #1a1a1a
    white = (255, 255, 255, 255)
    
    # Draw rounded rectangle background
    corner_radius = int(4 * scale)
    draw.rounded_rectangle([(0, 0), (work_size-1, work_size-1)], radius=corner_radius, fill=bg_color)
    
    # Draw microphone with thicker lines for better visibility
    line_width = max(2, int(2.5 * scale))
    
    # Microphone body parameters
    mic_x = int(12 * scale)
    mic_y_top = int(3 * scale)  # Slightly lower
    mic_width = int(5 * scale)  # Slightly narrower
    mic_height = int(9 * scale)  # Slightly shorter
    
    # Draw microphone capsule (filled)
    mic_left = mic_x - mic_width // 2
    mic_right = mic_x + mic_width // 2
    mic_bottom = mic_y_top + mic_height
    
    # Draw filled microphone body with rounded ends
    draw.rounded_rectangle(
        [(mic_left, mic_y_top), (mic_right, mic_bottom)],
        radius=mic_width // 2,
        fill=None,
        outline=white,
        width=line_width
    )
    
    # Draw microphone grille lines (optional detail)
    grille_y = mic_y_top + mic_height // 3
    for i in range(3):
        y = grille_y + i * int(scale * 1.5)
        if y < mic_bottom - mic_width // 2:
            draw.line(
                [(mic_left + line_width, y), (mic_right - line_width, y)],
                fill=white,
                width=max(1, int(scale * 0.5))
            )
    
    # Draw microphone arc (stand) with better curve
    arc_top = int(10 * scale)
    arc_bottom = int(16 * scale)
    arc_left = int(6 * scale)
    arc_right = int(18 * scale)
    
    # Use ellipse instead of arc for smoother curve
    draw.arc(
        [(arc_left, arc_top), (arc_right, arc_bottom)],
        start=20,
        end=160,
        fill=white,
        width=line_width
    )
    
    # Draw microphone stem/base
    stem_top = int(16 * scale)
    stem_bottom = int(21 * scale)
    draw.line(
        [(mic_x, stem_top), (mic_x, stem_bottom)],
        fill=white,
        width=line_width
    )
    
    # Draw base (horizontal line)
    base_width = int(4 * scale)
    base_y = int(21 * scale)
    draw.line(
        [(mic_x - base_width // 2, base_y), (mic_x + base_width // 2, base_y)],
        fill=white,
        width=line_width
    )
    
    # Downscale for better quality
    img = img.resize((size, size), Image.Resampling.LANCZOS)
    
    return img

# Generate high-quality PNG specifically for UI display
sizes_ui = [64, 128, 256]

for size in sizes_ui:
    img = create_high_quality_microphone_icon(size)
    img.save(os.path.join(output_dir, f'icon_rounded_{size}.png'))
    print(f"Generated icon_rounded_{size}.png")

# Generate the main UI icon at highest quality
img = create_high_quality_microphone_icon(256)
img.save(os.path.join(output_dir, 'icon_ui.png'))
print("Generated icon_ui.png (256x256)")

print(f"\nAll rounded icons generated in: {output_dir}")