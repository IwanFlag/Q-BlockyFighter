using UnityEngine;
using System.Collections.Generic;

namespace QBlockyFighter.Network
{
    /// <summary>
    /// 客户端预测与服务器回滚系统
    /// 本地立即执行输入，收到服务器帧后校验差异并修正
    /// 减少延迟感，保证确定性
    /// </summary>
    public class ClientPrediction : MonoBehaviour
    {
        [Header("预测配置")]
        public int maxPredictionFrames = 10;    // 最大预测帧数
        public float reconcileThreshold = 0.1f; // 位置修正阈值
        public float extrapolateLimit = 0.5f;   // 外推限制

        // 输入历史（用于回滚）
        private Dictionary<int, PredictedState> inputHistory = new Dictionary<int, PredictedState>();
        private int currentFrame;
        private int lastServerFrame;

        // 预测状态
        private Vector3 predictedPosition;
        private Quaternion predictedRotation;
        private bool isReconciling;

        public struct PredictedState
        {
            public int frame;
            public Vector3 position;
            public Quaternion rotation;
            public Dictionary<string, bool> inputs;
            public float timestamp;
        }

        void Update()
        {
            currentFrame++;
        }

        /// <summary>
        /// 记录本地预测输入
        /// 在发送输入到服务器前调用
        /// </summary>
        public void RecordPrediction(Vector3 pos, Quaternion rot, Dictionary<string, bool> inputs)
        {
            var state = new PredictedState
            {
                frame = currentFrame,
                position = pos,
                rotation = rot,
                inputs = inputs,
                timestamp = Time.time
            };

            inputHistory[currentFrame] = state;
            predictedPosition = pos;
            predictedRotation = rot;

            // 清理旧帧
            int cutoff = currentFrame - maxPredictionFrames * 2;
            var keysToRemove = new List<int>();
            foreach (var key in inputHistory.Keys)
            {
                if (key < cutoff) keysToRemove.Add(key);
            }
            foreach (var key in keysToRemove)
            {
                inputHistory.Remove(key);
            }
        }

        /// <summary>
        /// 收到服务器帧后，校验预测是否正确
        /// 如果差异过大，回滚并重新模拟
        /// </summary>
        public Vector3 Reconcile(int serverFrame, Vector3 serverPosition, Quaternion serverRotation, Transform playerTransform)
        {
            lastServerFrame = serverFrame;

            if (!inputHistory.TryGetValue(serverFrame, out var predicted))
            {
                // 没有该帧的预测记录，直接同步
                return serverPosition;
            }

            // 比较预测位置和服务器位置
            float positionError = Vector3.Distance(predicted.position, serverPosition);
            float rotationError = Quaternion.Angle(predicted.rotation, serverRotation);

            if (positionError < reconcileThreshold && rotationError < 5f)
            {
                // 预测正确，无需修正
                isReconciling = false;
                return playerTransform.position;
            }

            // 预测错误，需要回滚修正
            isReconciling = true;
            Debug.Log($"[预测] 帧{serverFrame} 位置误差: {positionError:F2}m, 开始回滚修正");

            // 从服务器正确位置开始，重新模拟后续帧
            Vector3 correctedPos = serverPosition;
            Quaternion correctedRot = serverRotation;

            // 重新应用从serverFrame到currentFrame的所有输入
            for (int f = serverFrame + 1; f <= currentFrame; f++)
            {
                if (inputHistory.TryGetValue(f, out var futureState))
                {
                    correctedPos = SimulateMovement(correctedPos, futureState.inputs, Time.fixedDeltaTime);
                    // 简化：不重新计算旋转
                }
            }

            // 平滑修正（避免瞬间跳跃）
            float smoothFactor = Mathf.Clamp01(positionError * 5f);
            Vector3 smoothed = Vector3.Lerp(playerTransform.position, correctedPos, smoothFactor);

            isReconciling = false;
            return smoothed;
        }

        /// <summary>简单的移动模拟（用于回滚重新计算）</summary>
        private Vector3 SimulateMovement(Vector3 pos, Dictionary<string, bool> inputs, float dt)
        {
            float speed = 5f;
            Vector3 move = Vector3.zero;

            if (inputs != null)
            {
                if (inputs.TryGetValue("w", out bool w) && w) move.z += 1;
                if (inputs.TryGetValue("s", out bool s) && s) move.z -= 1;
                if (inputs.TryGetValue("a", out bool a) && a) move.x -= 1;
                if (inputs.TryGetValue("d", out bool d) && d) move.x += 1;

                if (inputs.TryGetValue("shift", out bool shift) && shift) speed *= 2f; // 闪避
            }

            return pos + move.normalized * speed * dt;
        }

        /// <summary>外推预测（服务器帧未到达时）</summary>
        public Vector3 Extrapolate(Vector3 lastPos, Vector3 lastVelocity, float timeSinceLastFrame)
        {
            if (timeSinceLastFrame > extrapolateLimit) return lastPos;
            return lastPos + lastVelocity * timeSinceLastFrame;
        }

        /// <summary>检查是否正在修正</summary>
        public bool IsReconciling() => isReconciling;

        /// <summary>获取预测位置</summary>
        public Vector3 GetPredictedPosition() => predictedPosition;

        /// <summary>重置</summary>
        public void Reset()
        {
            inputHistory.Clear();
            currentFrame = 0;
            lastServerFrame = 0;
            isReconciling = false;
        }
    }
}
