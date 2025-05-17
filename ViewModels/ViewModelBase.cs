using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DLGameViewer.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 속성 값을 설정하고 PropertyChanged 이벤트를 발생시킵니다.
        /// </summary>
        /// <typeparam name="T">속성 타입</typeparam>
        /// <param name="storage">속성의 백킹 필드</param>
        /// <param name="value">새 값</param>
        /// <param name="propertyName">속성 이름 (자동으로 컴파일러가 채워줍니다)</param>
        /// <returns>값이 변경되었으면 true, 아니면 false</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// PropertyChanged 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        /// <summary>
        /// 새 RelayCommand를 생성합니다.
        /// </summary>
        /// <param name="execute">실행 작업</param>
        /// <param name="canExecute">명령 실행 가능 여부를 확인하는 함수 (선택사항)</param>
        /// <returns>새 RelayCommand 인스턴스</returns>
        protected ICommand CreateCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            return new RelayCommand(execute, canExecute);
        }
    }
} 