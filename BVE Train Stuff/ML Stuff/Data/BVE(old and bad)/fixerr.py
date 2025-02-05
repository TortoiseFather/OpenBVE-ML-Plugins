import os

# Paths
labels_dir = "dataset3/train/labels"  # Path to the labels directory
fixed_labels_dir = "dataset3/train/labels_fixed"  # Path to save fixed labels

# Ensure the output directory exists
os.makedirs(fixed_labels_dir, exist_ok=True)

# Process each label file
for label_file in os.listdir(labels_dir):
    input_path = os.path.join(labels_dir, label_file)
    output_path = os.path.join(fixed_labels_dir, label_file)

    # Open the original label file
    with open(input_path, "r") as f:
        lines = f.readlines()

    fixed_lines = []

    # Process each line
    for line in lines:
        line = line.strip().split()
        class_id = line[0]
        coords = line[1:]

        # Ensure the coordinates are in groups of 4 (YOLO bounding box format)
        if len(coords) % 4 != 0:
            print(f"Warning: Line in {label_file} has invalid format: {line}")
            continue

        # Split into multiple bounding boxes
        for i in range(0, len(coords), 4):
            bbox = coords[i:i+4]
            fixed_lines.append(f"{class_id} {' '.join(bbox)}\n")

    # Save the fixed labels
    with open(output_path, "w") as f:
        f.writelines(fixed_lines)

print(f"Label files have been fixed and saved to: {fixed_labels_dir}")
