using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.AR;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static OpenCVForUnity.UnityIntegration.Helper.Source2Mat.MultiSource2MatHelper;

namespace OpenCVForUnityExample
{
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class ArUcoBoxTest : MonoBehaviour
    {
        public enum MarkerType
        {
            CanonicalMarker
        }

        public enum ArUcoDictionary
        {
            DICT_6X6_250 = Objdetect.DICT_6X6_250
        }

        public float MarkerLength = 0.1f;

        public ARHelper ArHelper;
        public GameObject ArCubePrefab;

        private Texture2D _texture;
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _rgbMat;
        private Mat _camMatrix;
        private MatOfDouble _distCoeffs;

        private Mat _ids;
        private List<Mat> _corners;
        private List<Mat> _rejectedCorners;
        private Dictionary _dictionary;
        private ArucoDetector _arucoDetector;

        private Dictionary<int, ARGameObject> _arObjects = new Dictionary<int, ARGameObject>();

        private void Start()
        {
            OpenCVDebug.SetDebugMode(true);

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;
            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            if (!_multiSource2MatHelper.IsPlaying() || !_multiSource2MatHelper.DidUpdateThisFrame())
                return;

            Mat rgbaMat = _multiSource2MatHelper.GetMat();
            Imgproc.cvtColor(rgbaMat, _rgbMat, Imgproc.COLOR_RGBA2RGB);

            _arucoDetector.detectMarkers(_rgbMat, _corners, _ids, _rejectedCorners);

            if (_ids.total() > 0)
            {
                EstimatePose(rgbaMat);
            }

            Imgproc.cvtColor(_rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            foreach (var c in _corners) c.Dispose();
            _corners.Clear();
        }

        public void OnSourceToMatHelperInitialized()
        {
            Mat rgbaMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            gameObject.GetComponent<Renderer>().material.mainTexture = _texture;

            Camera.main.orthographicSize = _texture.height / 2f;
            transform.localScale = new Vector3(_texture.width, _texture.height, 1);

            double fx = rgbaMat.width();
            double fy = rgbaMat.width();
            double cx = rgbaMat.width() / 2.0f;
            double cy = rgbaMat.height() / 2.0f;

            _camMatrix = new Mat(3, 3, CvType.CV_64FC1);
            _camMatrix.put(0, 0, fx);
            _camMatrix.put(0, 1, 0);
            _camMatrix.put(0, 2, cx);
            _camMatrix.put(1, 0, 0);
            _camMatrix.put(1, 1, fy);
            _camMatrix.put(1, 2, cy);
            _camMatrix.put(2, 0, 0);
            _camMatrix.put(2, 1, 0);
            _camMatrix.put(2, 2, 1.0f);

            _distCoeffs = new MatOfDouble(0, 0, 0, 0);

            _rgbMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            _ids = new Mat();
            _corners = new List<Mat>();
            _rejectedCorners = new List<Mat>();

            _dictionary = Objdetect.getPredefinedDictionary((int)ArUcoDictionary.DICT_6X6_250);
            _arucoDetector = new ArucoDetector(_dictionary);

            ArHelper.Initialize();
            ArHelper.ARCamera.SetCamMatrix(_camMatrix);
            ArHelper.ARCamera.SetDistCoeffs(_distCoeffs);
            ArHelper.ARCamera.SetARCameraParameters(Screen.width, Screen.height, _rgbMat.width(), _rgbMat.height(), Vector2.zero, Vector2.one);
        }

        private void EstimatePose(Mat rgbaMat)
        {
            using (MatOfPoint3f objectPoints = new MatOfPoint3f(
                new Point3(-MarkerLength / 2f, MarkerLength / 2f, 0),
                new Point3(MarkerLength / 2f, MarkerLength / 2f, 0),
                new Point3(MarkerLength / 2f, -MarkerLength / 2f, 0),
                new Point3(-MarkerLength / 2f, -MarkerLength / 2f, 0)
            ))
            {
                int[] idsArray = new int[_ids.total()];
                _ids.get(0, 0, idsArray);

                for (int i = 0; i < idsArray.Length; i++)
                {
                    using (Mat corner = _corners[i].reshape(2, 4))
                    using (MatOfPoint2f imagePoints = new MatOfPoint2f(corner))
                    {
                        int id = idsArray[i];

                        if (!_arObjects.ContainsKey(id))
                        {
                            ARGameObject arObj = Instantiate(ArCubePrefab, ArHelper.transform).GetComponent<ARGameObject>();
                            arObj.gameObject.SetActive(true);
                            _arObjects[id] = arObj;
                            ArHelper.ARGameObjects.Add(arObj);
                        }

                        ARGameObject aRGameObject = _arObjects[id];
                        aRGameObject.ImagePoints = imagePoints.toVector2Array();
                        aRGameObject.ObjectPoints = objectPoints.toVector3Array();
                    }
                }
            }
        }
    }
}