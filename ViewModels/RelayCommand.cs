using System;
using System.Windows.Input;

namespace DLGameViewer.ViewModels
{
    /// <summary>
    /// 액션 대리자를 기반으로 한 ICommand 구현체입니다.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// RelayCommand 생성자
        /// </summary>
        /// <param name="execute">실행 시 호출될 액션</param>
        /// <param name="canExecute">명령 실행 가능 여부를 판단하는 조건(생략 가능)</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 명령이 현재 실행 가능한지 여부를 확인합니다.
        /// </summary>
        /// <param name="parameter">명령 매개변수</param>
        /// <returns>실행 가능 여부</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// 명령을 실행합니다.
        /// </summary>
        /// <param name="parameter">명령 매개변수</param>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
} 