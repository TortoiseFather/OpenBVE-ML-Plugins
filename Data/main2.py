from ultralytics import YOLO

# Load model
model = YOLO("yolov8n.pt")  # If you're using your custom weights, replace with "weights.pt"

# Train the model
model.train(
    data="nodynamic/data.yaml",
    epochs=50,
    imgsz=640,
    batch=16,
    name="yolov11-milestone-model",
    resume=False,
    device='cpu'  # or 'cpu' if needed
)
metrics = model.val()
print(metrics)  # Precision, Recall, mAP etc.
results = model.predict("path/to/test/image.jpg", conf=0.25)
results[0].show()
