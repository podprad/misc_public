using System.Collections;
using System.ComponentModel;

namespace TelerikValidationTooltipIssue
{
    public class MainViewModel : NotifyPropertyChangedObject, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> errorsDict = new Dictionary<string, List<string>>();

        private string _name;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public string Name
        {
            get => _name;

            set => SetField(ref _name, value);
        }

        public bool HasErrors => GetAllErrors().Any();

        public void AddErrors(string propertyName, string[] errors)
        {
            var list = GetErrorsForProperty(propertyName);
            list.AddRange(errors);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public void ClearErrors(string propertyName)
        {
            var list = GetErrorsForProperty(propertyName);
            list.Clear();
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            return GetErrorsForProperty(propertyName);
        }

        private IEnumerable<string> GetAllErrors()
        {
            foreach (var values in errorsDict.Values)
            {
                foreach (var value in values)
                {
                    yield return value;
                }
            }
        }

        private List<string> GetErrorsForProperty(string propertyName)
        {
            if (!errorsDict.TryGetValue(propertyName, out var list))
            {
                list = new List<string>();
                errorsDict[propertyName] = list;
            }

            return list;
        }
    }
}