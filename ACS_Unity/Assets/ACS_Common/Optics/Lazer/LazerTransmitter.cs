using ACS_Common.Base;
using UnityEngine;

namespace JSQZ_Common.Optics.Lazer
{
    [RequireComponent(typeof(LineRenderer))]
    public class LazerTransmitter : ACS_Behaviour
    {
        /// <summary>
        /// 射线长度限制
        /// </summary>
        [SerializeField] private float RayLengthLimit = 100 * 1000f;
        /// <summary>
        /// 最大反射次数限制
        /// </summary>
        [SerializeField] private int ReflectionCntLimit = 20;
        /// <summary>
        /// 反射激光的物体层级
        /// </summary>
        [SerializeField] LayerMask _reflectLayers = -1;
        /// <summary>
        /// 吸收激光的物体层级
        /// </summary>
        [SerializeField] LayerMask _absorbLayers = -1;
        /// <summary>
        /// 是否显示吸收点准星
        /// </summary>
        [SerializeField] private bool _showAbsorbReticle;
        /// <summary>
        /// 吸收点准星
        /// </summary>
        [SerializeField] private GameObject _absorbReticle;
        
        /// <summary>
        /// 光路路径
        /// </summary>
        private Vector3[] _points;
        /// <summary>
        /// 吸收点
        /// </summary>
        private Vector3 _absorbPoint;
        /// <summary>
        /// 吸收点法线
        /// </summary>
        private Vector3 _absorbNormal;
        private LineRenderer _lr;

        private void Awake()
        {
            Debug.Log($"{Tag} Awake, gameObject: {gameObject}, length: {RayLengthLimit} mm, reflectionLayers: {_reflectLayers}, absorbLayers: {_absorbLayers}, reflectionLimit: {ReflectionCntLimit}");
            _points = new Vector3[ReflectionCntLimit + 2];
            Debug.Log($"LazerTransmitter, _points[0]: {_points[0]}");
            _lr = GetComponent<LineRenderer>();
            if (null == _lr)
            {
                Debug.LogError("LazerTransmitter LineRenderer missing");
            }
        }
        private void Update()
        {
            if (null == _lr) return;
            Raycast();
        }
        /// <summary>
        /// 投射激光
        /// </summary>
        private void Raycast()
        {
            // Debug.Log("LazerTransmitter Raycast begin");
            var rayLength = RayLengthLimit;
            var lastHitPos = transform.position;
            var lastDir = transform.forward;
            var reflectionCnt = 0;
            _points[0] = transform.position;
            while (reflectionCnt <= ReflectionCntLimit)
            {
                reflectionCnt++;
                Debug.Log($"LazerTransmitter Raycast loop begin, reflectionCnt: {reflectionCnt}, lastHitPos: {lastHitPos}, lastDir: {lastDir}");
                if (Physics.Raycast(lastHitPos, lastDir, out var hit, rayLength, _reflectLayers | _absorbLayers))
                {
                    if (((1 << hit.collider.gameObject.layer) & _absorbLayers) != 0)
                    {
                        // 激光结束
                        _absorbPoint = hit.point;
                        _absorbNormal = hit.normal;
                        _points[reflectionCnt] = _absorbPoint;
                        Debug.Log($"LazerTransmitter Raycast absorb, reflectionCnt: {reflectionCnt}, _points[reflectionCnt]: {_points[reflectionCnt]}");
                        break;
                    }
                    else if (((1 << hit.collider.gameObject.layer) & _reflectLayers) != 0)
                    {
                        // 激光反射
                        lastHitPos = hit.point;
                        lastDir = Vector3.Reflect(lastDir, hit.normal);
                        _points[reflectionCnt] = lastHitPos;
                        Debug.Log($"LazerTransmitter Raycast reflect, reflectionCnt: {reflectionCnt}, _points[reflectionCnt]: {_points[reflectionCnt]}");
                        rayLength -= hit.distance;
                    }
                }
                else
                {
                    _points[reflectionCnt] = lastHitPos + lastDir * rayLength;
                    rayLength = 0f;
                    Debug.Log($"LazerTransmitter Raycast not hit, reflectionCnt: {reflectionCnt}, _points[reflectionCnt]: {_points[reflectionCnt]}");
                    break;
                }
            }
            _lr.positionCount = reflectionCnt + 1;
            _lr.SetPositions(_points);

            if (null != _absorbReticle)
            {
                _absorbReticle.SetActive(_showAbsorbReticle && rayLength > 0f);
                if (_showAbsorbReticle && rayLength > 0f)
                {
                    _absorbReticle.SetActive(true);
                    _absorbReticle.transform.position = _absorbPoint + _absorbNormal * 0.001f;
                    _absorbReticle.transform.forward = -_absorbNormal;
                }
            }
            // Debug.Log("LazerTransmitter Raycast end");
        }
    }
}