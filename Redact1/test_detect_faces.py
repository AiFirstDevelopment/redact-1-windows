#!/usr/bin/env python3
"""Tests for face detection script."""
import json
import os
import sys
import tempfile
import unittest
from unittest.mock import patch, MagicMock
import numpy as np

# Import the module under test
import detect_faces


class TestDetectFaces(unittest.TestCase):
    """Test cases for detect_faces module."""

    def test_detect_faces_with_invalid_image_path(self):
        """Test that invalid image path returns error."""
        result = json.loads(detect_faces.detect_faces("/nonexistent/image.jpg", "/some/cascade.xml"))
        self.assertIn("error", result)
        self.assertEqual(result["faces"], [])

    def test_detect_faces_with_invalid_cascade_path(self):
        """Test that invalid cascade path returns error."""
        # Create a temporary valid image
        with tempfile.NamedTemporaryFile(suffix=".png", delete=False) as f:
            # Create a minimal 1x1 black image using numpy
            try:
                import cv2
                img = np.zeros((100, 100, 3), dtype=np.uint8)
                cv2.imwrite(f.name, img)

                result = json.loads(detect_faces.detect_faces(f.name, "/nonexistent/cascade.xml"))
                self.assertIn("error", result)
                self.assertEqual(result["faces"], [])
            finally:
                os.unlink(f.name)

    @patch('detect_faces.cv2')
    def test_detect_faces_returns_normalized_coordinates(self, mock_cv2):
        """Test that face coordinates are normalized to 0-1 range."""
        # Mock image loading
        mock_img = MagicMock()
        mock_img.shape = (100, 200, 3)  # height=100, width=200
        mock_cv2.imread.return_value = mock_img

        # Mock cascade
        mock_cascade = MagicMock()
        mock_cascade.empty.return_value = False
        mock_cascade.detectMultiScale.return_value = np.array([[20, 10, 40, 50]])  # x, y, w, h
        mock_cv2.CascadeClassifier.return_value = mock_cascade

        # Mock grayscale conversion
        mock_cv2.cvtColor.return_value = MagicMock()
        mock_cv2.equalizeHist.return_value = MagicMock()

        result = json.loads(detect_faces.detect_faces("test.jpg", "cascade.xml"))

        self.assertEqual(len(result["faces"]), 1)
        face = result["faces"][0]
        # x=20/200=0.1, y=10/100=0.1, w=40/200=0.2, h=50/100=0.5
        self.assertAlmostEqual(face["x"], 0.1)
        self.assertAlmostEqual(face["y"], 0.1)
        self.assertAlmostEqual(face["width"], 0.2)
        self.assertAlmostEqual(face["height"], 0.5)

    @patch('detect_faces.cv2')
    def test_detect_faces_filters_detections_below_another(self, mock_cv2):
        """Test that detections directly below another face are filtered out."""
        # Mock image loading
        mock_img = MagicMock()
        mock_img.shape = (400, 300, 3)  # height=400, width=300
        mock_cv2.imread.return_value = mock_img

        # Mock cascade - returns two faces, one below the other (simulating chest false positive)
        mock_cascade = MagicMock()
        mock_cascade.empty.return_value = False
        # Face at (100, 50, 80, 80) and false positive at (100, 150, 80, 80) - directly below
        mock_cascade.detectMultiScale.return_value = np.array([
            [100, 50, 80, 80],   # Real face
            [100, 150, 80, 80],  # False positive below
        ])
        mock_cv2.CascadeClassifier.return_value = mock_cascade

        mock_cv2.cvtColor.return_value = MagicMock()
        mock_cv2.equalizeHist.return_value = MagicMock()

        result = json.loads(detect_faces.detect_faces("test.jpg", "cascade.xml"))

        # Should only have 1 face (the false positive should be filtered)
        self.assertEqual(len(result["faces"]), 1)
        self.assertEqual(result["count"], 1)

    @patch('detect_faces.cv2')
    def test_detect_faces_keeps_non_overlapping_faces(self, mock_cv2):
        """Test that non-overlapping faces are all kept."""
        # Mock image loading
        mock_img = MagicMock()
        mock_img.shape = (300, 400, 3)  # height=300, width=400
        mock_cv2.imread.return_value = mock_img

        # Mock cascade - returns two faces side by side (both should be kept)
        mock_cascade = MagicMock()
        mock_cascade.empty.return_value = False
        mock_cascade.detectMultiScale.return_value = np.array([
            [50, 50, 80, 80],   # Left face
            [250, 50, 80, 80],  # Right face (different x, same y)
        ])
        mock_cv2.CascadeClassifier.return_value = mock_cascade

        mock_cv2.cvtColor.return_value = MagicMock()
        mock_cv2.equalizeHist.return_value = MagicMock()

        result = json.loads(detect_faces.detect_faces("test.jpg", "cascade.xml"))

        # Should have both faces
        self.assertEqual(len(result["faces"]), 2)
        self.assertEqual(result["count"], 2)

    @patch('detect_faces.cv2')
    def test_detect_faces_returns_count(self, mock_cv2):
        """Test that result includes face count."""
        mock_img = MagicMock()
        mock_img.shape = (100, 100, 3)
        mock_cv2.imread.return_value = mock_img

        mock_cascade = MagicMock()
        mock_cascade.empty.return_value = False
        mock_cascade.detectMultiScale.return_value = np.array([[10, 10, 30, 30]])
        mock_cv2.CascadeClassifier.return_value = mock_cascade

        mock_cv2.cvtColor.return_value = MagicMock()
        mock_cv2.equalizeHist.return_value = MagicMock()

        result = json.loads(detect_faces.detect_faces("test.jpg", "cascade.xml"))

        self.assertIn("count", result)
        self.assertEqual(result["count"], 1)

    @patch('detect_faces.cv2')
    def test_detect_faces_no_faces_returns_empty_list(self, mock_cv2):
        """Test that no faces returns empty list."""
        mock_img = MagicMock()
        mock_img.shape = (100, 100, 3)
        mock_cv2.imread.return_value = mock_img

        mock_cascade = MagicMock()
        mock_cascade.empty.return_value = False
        mock_cascade.detectMultiScale.return_value = np.array([]).reshape(0, 4)
        mock_cv2.CascadeClassifier.return_value = mock_cascade

        mock_cv2.cvtColor.return_value = MagicMock()
        mock_cv2.equalizeHist.return_value = MagicMock()

        result = json.loads(detect_faces.detect_faces("test.jpg", "cascade.xml"))

        self.assertEqual(result["faces"], [])
        self.assertEqual(result["count"], 0)

    @patch('detect_faces.cv2')
    def test_detect_faces_keeps_stacked_faces_different_x(self, mock_cv2):
        """Test that vertically stacked faces with different x positions are kept."""
        mock_img = MagicMock()
        mock_img.shape = (400, 400, 3)
        mock_cv2.imread.return_value = mock_img

        # Two faces stacked but not overlapping in x
        mock_cascade = MagicMock()
        mock_cascade.empty.return_value = False
        mock_cascade.detectMultiScale.return_value = np.array([
            [50, 50, 80, 80],    # Top-left face
            [250, 200, 80, 80],  # Bottom-right face (different x, below)
        ])
        mock_cv2.CascadeClassifier.return_value = mock_cascade

        mock_cv2.cvtColor.return_value = MagicMock()
        mock_cv2.equalizeHist.return_value = MagicMock()

        result = json.loads(detect_faces.detect_faces("test.jpg", "cascade.xml"))

        # Should keep both - they don't overlap in x
        self.assertEqual(len(result["faces"]), 2)


class TestMainFunction(unittest.TestCase):
    """Test the main entry point."""

    def test_main_with_wrong_args_prints_usage(self):
        """Test that wrong number of args returns usage error in JSON."""
        import subprocess
        result = subprocess.run(
            ['python3', 'detect_faces.py'],
            capture_output=True,
            text=True,
            cwd=os.path.dirname(os.path.abspath(__file__))
        )
        output = json.loads(result.stdout)
        self.assertIn("error", output)
        self.assertIn("Usage", output["error"])
        self.assertEqual(result.returncode, 1)

    def test_main_with_correct_args_runs(self):
        """Test that correct args run detection."""
        import subprocess
        # This will fail because files don't exist, but it should run
        result = subprocess.run(
            ['python3', 'detect_faces.py', '/nonexistent/image.png', '/nonexistent/cascade.xml'],
            capture_output=True,
            text=True,
            cwd=os.path.dirname(os.path.abspath(__file__))
        )
        output = json.loads(result.stdout)
        # Should return error about image, not usage error
        self.assertIn("error", output)
        self.assertNotIn("Usage", output["error"])


if __name__ == '__main__':
    unittest.main()
