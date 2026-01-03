using System;
using UnityEngine;

namespace Project.Scripts.Core.Managers
{
    public class SystemRoot : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}