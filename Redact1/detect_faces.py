#!/usr/bin/env python3
"""Face detection script using Amazon Rekognition."""
import sys
import json
import os

def detect_faces(image_path, cascade_path):
    """Detect faces in an image using Amazon Rekognition."""
    try:
        import boto3
        from botocore.exceptions import NoCredentialsError, ClientError
    except ImportError:
        return json.dumps({"error": "boto3 not installed", "faces": []})

    # Read image file
    try:
        with open(image_path, 'rb') as f:
            image_bytes = f.read()
    except Exception as e:
        return json.dumps({"error": f"Failed to read image: {str(e)}", "faces": []})

    # Get image dimensions using PIL or cv2
    try:
        import cv2
        import numpy as np
        nparr = np.frombuffer(image_bytes, np.uint8)
        img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        if img is None:
            return json.dumps({"error": "Failed to decode image", "faces": []})
        height, width = img.shape[:2]
    except Exception as e:
        return json.dumps({"error": f"Failed to get image dimensions: {str(e)}", "faces": []})

    # Call Amazon Rekognition
    try:
        client = boto3.client('rekognition')
        response = client.detect_faces(
            Image={'Bytes': image_bytes},
            Attributes=['DEFAULT']
        )
    except NoCredentialsError:
        return json.dumps({"error": "AWS credentials not configured. Run: aws configure", "faces": []})
    except ClientError as e:
        return json.dumps({"error": f"AWS error: {str(e)}", "faces": []})
    except Exception as e:
        return json.dumps({"error": f"Rekognition error: {str(e)}", "faces": []})

    # Convert to normalized coordinates
    result = []
    for face in response.get('FaceDetails', []):
        bbox = face['BoundingBox']
        result.append({
            "x": bbox['Left'],
            "y": bbox['Top'],
            "width": bbox['Width'],
            "height": bbox['Height']
        })

    return json.dumps({"faces": result, "count": len(result)})

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(json.dumps({"error": "Usage: detect_faces.py <image_path> <cascade_path>", "faces": []}))
        sys.exit(1)

    print(detect_faces(sys.argv[1], sys.argv[2]))
