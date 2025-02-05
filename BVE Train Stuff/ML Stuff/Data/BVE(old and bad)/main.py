from ultralytics import YOLO
import torch

print("CUDA available:", torch.cuda.is_available())
print("Device count:", torch.cuda.device_count())
if torch.cuda.is_available():
    print("Using device:", torch.cuda.get_device_name(0))
else:
    print("No GPU detected!")


model = YOLO("yolo11n.pt")
results = model.train(data="Dataset3/data.yaml", epochs=100, imgsz=640)
model.export(format='onnx', imgsz=640)
ls