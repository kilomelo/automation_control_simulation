using System;
using System.Text;
using UnityEngine;

namespace ACS_Common.Utils
{
    public static class StringUtils
    {
        private static StringBuilder _sb_ProgressBar = new StringBuilder();
        public static string ProgressBar(float progress, int digits = 0, char complete = '#', char incomplete = '-')
        {
            // Debug.Log($"ProgressBar, progress: [{progress}], f: [{f}]");
            progress = Math.Clamp(progress, 0f, 1f);
            var incompleteInt = 100 - (int)Math.Floor(progress * 100);
            var interval = 10;
            while (digits > 0)
            {
                progress *= 10;
                interval *= 10;
                digits--;
            }
            var progressInt = (int)(progress * 100);
            // Debug.Log($"ProgressBar, progressInt: [{progressInt}], d: [{d}]");
            _sb_ProgressBar.Clear();
            while (progressInt >= interval)
            {
                progressInt -= interval;
                // Debug.Log($"ProgressBar, progressInt: [{progressInt}]");
                _sb_ProgressBar.Append(complete);
                // if (progressInt <= 0) break;
            }
            // Debug.Log($"ProgressBar, progressInt: [{progressInt}]");
            if (0 != progressInt)
            {
                _sb_ProgressBar.Append(progressInt);
                incompleteInt -= 10;
            }
            while (incompleteInt > 0)
            {
                incompleteInt -= 10;
                _sb_ProgressBar.Append(incomplete);
            }
            return _sb_ProgressBar.ToString();
        }

        public static void Test_ProgressBar()
        {
            var testProgress = new float[] {0f, 1f, 0.54321f, 0.34567f, -0.1f, 1.1f};
            var fArray = new int[] {0, 1, 2};
            foreach (var f in fArray)
            {
                Debug.Log($"Test_ProgressBar, f [{f}]");
                foreach (var p in testProgress)
                {
                    var bar = ProgressBar(p, f);
                    Debug.Log($"Test_ProgressBar, p: [{p}], bar: [{bar}], len: {bar.Length}");
                }
            }
        }
    }
}