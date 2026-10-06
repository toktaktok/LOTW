using UnityEngine;
using System;

namespace Project.Scripts.Framework
{
    public class StateMachine<T> where T : class
    {
        private readonly T _owner;
        private Action _currentState;
        private string _currentStateName;
        private Action _exitState;
        
        public string CurrentStateName => _currentStateName;
        
        public StateMachine(T owner)
        {
            _owner = owner;
        }

        public void ChangeState(string stateName, Action onEnter, Action onUpdate, Action onExit = null)
        {
            if(_exitState != null)
                _exitState();
            
            _currentStateName = stateName;
            onEnter?.Invoke();
            _currentState = onUpdate;
            _exitState = onExit;
        }

        public void Update()
        {
            _currentState?.Invoke();
        }
    }
}