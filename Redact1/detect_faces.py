#!/usr/bin/env python3
"""Face detection script using OpenCV Haar cascade."""
import sys
import cv2
import json

def detect_faces(image_path, cascade_path):
    """Detect faces in an image and return bounding boxes as JSON."""
    # Load image
    img = cv2.imread(image_path)
    if img is None:
        return json.dumps({"error": "Failed to load image", "faces": []})

    height, width = img.shape[:2]

    # Load cascade
    face_cascade = cv2.CascadeClassifier(cascade_path)
    if face_cascade.empty():
        return json.dumps({"error": "Failed to load cascade", "faces": []})

    # Convert to grayscale
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    gray = cv2.equalizeHist(gray)

    # Detect faces
    faces = face_cascade.detectMultiScale(
        gray,
        scaleFactor=1.1,
        minNeighbors=5,
        minSize=(30, 30)
    )

    # Convert to normalized coordinates
    result = []
    for (x, y, w, h) in faces:
        result.append({
            "x": x / width,
            "y": y / height,
            "width": w / width,
            "height": h / height
        })

    return json.dumps({"faces": result, "count": len(result)})

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(json.dumps({"error": "Usage: detect_faces.py <image_path> <cascade_path>", "faces": []}))
        sys.exit(1)

    print(detect_faces(sys.argv[1], sys.argv[2]))
