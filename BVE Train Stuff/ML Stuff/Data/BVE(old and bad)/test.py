import os
import cv2

# Paths to the dataset
dataset_path = "dataset3"  # Path to the dataset folder
subset = "train"  # Change to "test" or "valid" as needed

# Load data.yaml to extract class names
import yaml
with open(os.path.join(dataset_path, "data.yaml"), "r") as f:
    data_yaml = yaml.safe_load(f)
class_names = data_yaml.get("names", [])

# Define paths for images and labels
images_path = os.path.join(dataset_path, subset, "images")
labels_path = os.path.join(dataset_path, subset, "labels_fixed")

# Function to visualize a single image
def visualize_image(image_path, label_path, class_names, include_confidence=False):
    """
    Visualizes an image with bounding boxes and optionally confidence scores.

    Args:
        image_path (str): Path to the image file.
        label_path (str): Path to the corresponding label file.
        class_names (list): List of class names.
        include_confidence (bool): If True, includes confidence scores.
    """
    # Load the image
    image = cv2.imread(image_path)
    h, w, _ = image.shape

    # Check if corresponding label file exists
    if not os.path.exists(label_path):
        print(f"Label file not found for image: {image_path}")
        return

    # Parse the label file
    with open(label_path, "r") as f:
        for line in f.readlines():
            line = line.strip().split()
            class_id = int(line[0])
            x_center, y_center, box_width, box_height = map(float, line[1:5])

            # Handle confidence score if included in labels
            confidence = None
            if include_confidence and len(line) > 5:
                confidence = float(line[5])

            # Denormalize bounding box
            x1 = int((x_center - box_width / 2) * w)
            y1 = int((y_center - box_height / 2) * h)
            x2 = int((x_center + box_width / 2) * w)
            y2 = int((y_center + box_height / 2) * h)

            # Construct label with confidence if applicable
            label = f"{class_names[class_id]}"
            if confidence is not None:
                label += f" ({confidence:.2f})"

            # Draw the bounding box and label
            cv2.rectangle(image, (x1, y1), (x2, y2), (0, 255, 0), 2)
            cv2.putText(image, label, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)

    # Display the image
    cv2.imshow("Image with Bounding Boxes", image)
    cv2.waitKey(0)
    cv2.destroyAllWindows()

# Iterate through images in the subset
for image_file in os.listdir(images_path):
    if image_file.endswith((".jpg", ".png", ".jpeg")):
        image_path = os.path.join(images_path, image_file)
        label_file = os.path.splitext(image_file)[0] + ".txt"
        label_path = os.path.join(labels_path, label_file)
        visualize_image(image_path, label_path, class_names, include_confidence=True)
