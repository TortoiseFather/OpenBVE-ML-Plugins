import cv2
import numpy as np
import onnxruntime as ort

# Configuration
video_path = "vlc-record-2025-01-09-16h03m05s-2025-01-09 15-59-06.mkv-.mp4"  # Path to the input video
output_video_path = "output_video.avi"  # Path to save the output video
model_path = "bestBVEOnly.onnx"  # Path to your YOLO ONNX model
input_size = (640, 640)  # Model's expected input size (W, H)
conf_threshold = 0.00001
iou_threshold = 0.00004

# Load the ONNX model
session = ort.InferenceSession(model_path)

# Get model input and output names
input_name = session.get_inputs()[0].name
output_name = session.get_outputs()[0].name


# Open video file
cap = cv2.VideoCapture(video_path)
width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
fps = int(cap.get(cv2.CAP_PROP_FPS))
out = cv2.VideoWriter(output_video_path, cv2.VideoWriter_fourcc(*"XVID"), fps, (width, height))

# Preprocessing function
def preprocess(frame, input_size=(640, 640)):
    # Resize frame with padding
    h, w, c = frame.shape
    scale = min(input_size[0] / w, input_size[1] / h)
    nw, nh = int(scale * w), int(scale * h)
    image_resized = cv2.resize(frame, (nw, nh))

    # Create a blank canvas and place the resized image
    canvas = np.full((input_size[1], input_size[0], c), 128, dtype=np.uint8)  # Gray background
    dw, dh = (input_size[0] - nw) // 2, (input_size[1] - nh) // 2
    canvas[dh:dh + nh, dw:dw + nw] = image_resized

    # Normalize and reshape
    blob = canvas / 255.0  # Normalize to [0, 1]
    blob = blob.transpose(2, 0, 1)  # Change to (C, H, W)
    blob = np.expand_dims(blob, axis=0).astype(np.float32)  # Add batch dimension
    return blob, scale, dw, dh

# Postprocessing function
def postprocess(outputs, original_frame, scale, dw, dh, conf_threshold=0.5, iou_threshold=0.4):
    h, w = original_frame.shape[:2]
    detections = outputs[0].squeeze()  # Shape: (9, 8400)

    boxes, scores, class_ids = [], [], []

    for detection in detections.T:
        cx, cy, width, height, confidence = detection[:5]
        class_scores = detection[5:]
        class_id = np.argmax(class_scores)
        class_score = class_scores[class_id]

        if confidence > conf_threshold and class_score > conf_threshold:
            # Convert bbox to original scale and adjust for padding
            x1 = int((cx - width / 2 - dw) / scale)
            y1 = int((cy - height / 2 - dh) / scale)
            x2 = int((cx + width / 2 - dw) / scale)
            y2 = int((cy + height / 2 - dh) / scale)

            # Ensure the box is within frame boundaries
            x1, y1, x2, y2 = max(0, x1), max(0, y1), min(w, x2), min(h, y2)
            boxes.append([x1, y1, x2 - x1, y2 - y1])  # Box as [x, y, width, height]
            scores.append(class_score)
            class_ids.append(class_id)

    # Apply NMS
    indices = cv2.dnn.NMSBoxes(boxes, scores, conf_threshold, iou_threshold)

    result_boxes, result_scores, result_class_ids = [], [], []
    if len(indices) > 0:
        for i in indices.flatten():
            result_boxes.append(boxes[i])
            result_scores.append(scores[i])
            result_class_ids.append(class_ids[i])

    return result_boxes, result_scores, result_class_ids

while cap.isOpened():
    ret, frame = cap.read()
    if not ret:
        break

    # Preprocess the frame
    input_blob, scale, dw, dh = preprocess(frame)

    # Run inference
    outputs = session.run([output_name], {input_name: input_blob})

    # Postprocess detections
    boxes, scores, class_ids = postprocess(outputs, frame, scale, dw, dh, conf_threshold=0.1)

    # Draw detections on the frame
    for box, score, class_id in zip(boxes, scores, class_ids):
        x, y, w, h = box
        label = f"Class {class_id}: {score:.2f}"
        cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 255, 0), 2)
        cv2.putText(frame, label, (x, y - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)

    # Write the frame to the output video
    out.write(frame)

    # Optionally display the frame
    cv2.imshow("YOLO Detection", frame)
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# Release resources

cap.release()
out.release()
cv2.destroyAllWindows()
