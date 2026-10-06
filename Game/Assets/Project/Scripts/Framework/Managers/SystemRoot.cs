using System;
using UnityEngine;

namespace Project.Scripts.Framework.Managers
{
    public class SystemRoot : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}