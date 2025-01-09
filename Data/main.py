import cv2
import numpy as np
import onnxruntime as ort

# Configuration
video_path = "vlc-record-2025-01-09-16h03m05s-2025-01-09 15-59-06.mkv-.mp4"  # Path to the input video
output_video_path = "output_video.avi"  # Path to save the output video
model_path = "best.onnx"  # Path to your YOLO ONNX model
input_size = (640, 640)  # Model's expected input size (W, H)
class_names = ["Class 0", "Class 1", "Class 2", "Class 3", "Class 4", "Class 5", "Class 6", "Class 7",
                       "Class 8"]

# Load the ONNX model
session = ort.InferenceSession(model_path)

# Get model input and output names
input_name = session.get_inputs()[0].name
output_names = [output.name for output in session.get_outputs()]

# Open video file
cap = cv2.VideoCapture(video_path)
width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
fps = int(cap.get(cv2.CAP_PROP_FPS))
out = cv2.VideoWriter(output_video_path, cv2.VideoWriter_fourcc(*"XVID"), fps, (width, height))

def preprocess(frame):
    # Resize and normalize the frame
    resized = cv2.resize(frame, input_size)
    blob = resized / 255.0  # Normalize to [0, 1]
    blob = blob.transpose(2, 0, 1)  # Change to (C, H, W)
    blob = np.expand_dims(blob, axis=0).astype(np.float32)  # Add batch dimension
    return blob

def postprocess(outputs, original_frame, conf_threshold=0.5, iou_threshold=0.4):
    """
    Postprocess the outputs of the YOLO model to extract bounding boxes, class IDs, and confidence scores.
    """
    boxes, scores, class_ids = [], [], []
    h, w = original_frame.shape[:2]

    # Parse each detection from model output
    for detection in outputs[0]:  # Assuming the output format is [x, y, w, h, conf, cls_1, cls_2, ...]
        # Extract objectness score (confidence)
        confidence = float(detection[4])  # Convert to scalar

        # If objectness confidence meets threshold
        if confidence > conf_threshold:
            # Extract class scores
            class_scores = detection[5:]  # Class probabilities
            class_id = np.argmax(class_scores)  # Get class ID with highest probability
            class_score = class_scores[class_id]  # Get the probability for the detected class

            # If the class probability also meets the threshold
            if class_score > conf_threshold:
                # Convert bbox format (center_x, center_y, width, height) to (x1, y1, x2, y2)
                center_x, center_y, box_w, box_h = detection[:4]
                x1 = int((center_x - box_w / 2) * w)
                y1 = int((center_y - box_h / 2) * h)
                x2 = int((center_x + box_w / 2) * w)
                y2 = int((center_y + box_h / 2) * h)

                boxes.append([x1, y1, x2 - x1, y2 - y1])  # Box as [x, y, width, height]
                scores.append(class_score)  # Append the class confidence score
                class_ids.append(class_id)  # Append the class ID

    # Apply Non-Maximum Suppression (NMS) to remove redundant overlapping boxes
    indices = cv2.dnn.NMSBoxes(boxes, scores, conf_threshold, iou_threshold)

    result_boxes, result_scores, result_class_ids = [], [], []
    if len(indices) > 0:
        for i in indices.flatten():
            result_boxes.append(boxes[i])
            result_scores.append(scores[i])
            result_class_ids.append(class_ids[i])

    return result_boxes, result_scores, result_class_ids


# Read video frame by frame
while cap.isOpened():
    ret, frame = cap.read()
    if not ret:
        break

    # Preprocess the frame
    input_blob = preprocess(frame)

    # Run inference
    outputs = session.run(output_names, {input_name: input_blob})

    # Postprocess detections
    boxes, scores, class_ids = postprocess(outputs, frame)

    # Draw detections
    for box, score, class_id in zip(boxes, scores, class_ids):

        x, y, w, h = box
        label = f"Class {class_id}: {score:.2f}"
        cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 255, 0), 2)
        cv2.putText(frame, label, (x, y - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)

    # Write frame to output video
    out.write(frame)

    # Optionally display the frame
    cv2.imshow("YOLO Detection", frame)
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# Release resources
cap.release()
out.release()
cv2.destroyAllWindows()
