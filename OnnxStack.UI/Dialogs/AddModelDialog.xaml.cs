﻿using Microsoft.Extensions.Logging;
using OnnxStack.Core;
using OnnxStack.Core.Config;
using OnnxStack.StableDiffusion.Config;
using OnnxStack.StableDiffusion.Enums;
using OnnxStack.UI.Commands;
using OnnxStack.UI.Models;
using OnnxStack.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace OnnxStack.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for AddModelDialog.xaml
    /// </summary>
    public partial class AddModelDialog : Window, INotifyPropertyChanged
    {
        private readonly ILogger<AddModelDialog> _logger;

        private List<string> _invalidOptions;
        private DiffuserPipelineType _pipelineType;
        private ModelType _modelType;
        private string _modelFolder;
        private string _modelName;
        private IModelFactory _modelFactory;

        public AddModelDialog(IModelFactory modelFactory, ILogger<AddModelDialog> logger)
        {
            _logger = logger;
            _modelFactory = modelFactory;
            WindowCloseCommand = new AsyncRelayCommand(WindowClose);
            WindowRestoreCommand = new AsyncRelayCommand(WindowRestore);
            WindowMinimizeCommand = new AsyncRelayCommand(WindowMinimize);
            WindowMaximizeCommand = new AsyncRelayCommand(WindowMaximize);
            SaveCommand = new AsyncRelayCommand(Save, CanExecuteSave);
            CancelCommand = new AsyncRelayCommand(Cancel, CanExecuteCancel);
            InitializeComponent();
        }
        public AsyncRelayCommand WindowMinimizeCommand { get; }
        public AsyncRelayCommand WindowRestoreCommand { get; }
        public AsyncRelayCommand WindowMaximizeCommand { get; }
        public AsyncRelayCommand WindowCloseCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }

        public ObservableCollection<ValidationResult> ValidationResults { get; set; } = new ObservableCollection<ValidationResult>();

        public DiffuserPipelineType PipelineType
        {
            get { return _pipelineType; }
            set
            {
                _pipelineType = value;
                NotifyPropertyChanged();
                if (_pipelineType != DiffuserPipelineType.StableDiffusionXL && _pipelineType != DiffuserPipelineType.LatentConsistencyXL)
                {
                    _modelType = ModelType.Base;
                    NotifyPropertyChanged(nameof(ModelType));
                }
                CreateModelSet();
            }
        }


        public ModelType ModelType
        {
            get { return _modelType; }
            set { _modelType = value; NotifyPropertyChanged(); CreateModelSet(); }
        }

        public string ModelName
        {
            get { return _modelName; }
            set { _modelName = value; NotifyPropertyChanged(); CreateModelSet(); }
        }


        public string ModelFolder
        {
            get { return _modelFolder; }
            set
            {
                _modelFolder = value;
                _modelName = string.IsNullOrEmpty(_modelFolder)
                    ? string.Empty
                    : Path.GetFileName(_modelFolder);

                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ModelName));
                CreateModelSet();
            }
        }

        private bool _isNameInvalid;

        public bool IsNameInvalid
        {
            get { return _isNameInvalid; }
            set { _isNameInvalid = value; NotifyPropertyChanged(); }
        }


        private StableDiffusionModelSet _modelSet;

        public StableDiffusionModelSet ModelSet
        {
            get { return _modelSet; }
            set { _modelSet = value; NotifyPropertyChanged(); }
        }



        private void CreateModelSet()
        {
            ModelSet = null;
            IsNameInvalid = false;
            ValidationResults.Clear();
            if (string.IsNullOrEmpty(_modelFolder))
                return;

            ModelSet = _modelFactory.CreateModelSet(ModelName.Trim(), ModelFolder, PipelineType, ModelType);

            // Validate
            IsNameInvalid = !InvalidOptions.IsNullOrEmpty() && InvalidOptions.Contains(_modelName);
            foreach (var validationResult in ModelSet.ModelConfigurations.Select(x => new ValidationResult(x.Type, File.Exists(x.OnnxModelPath))))
            {
                ValidationResults.Add(validationResult);
            }
        }


        public List<string> InvalidOptions
        {
            get { return _invalidOptions; }
            set { _invalidOptions = value; NotifyPropertyChanged(); }
        }


        public bool ShowDialog(List<string> invalidOptions = null)
        {
            InvalidOptions = invalidOptions;
            return base.ShowDialog() ?? false;
        }


        private Task Save()
        {
            DialogResult = true;
            return Task.CompletedTask;
        }

        private bool CanExecuteSave()
        {
            if (string.IsNullOrEmpty(_modelFolder))
                return false;
            if (string.IsNullOrEmpty(_modelName) || IsNameInvalid)
                return false;
            if (_modelSet is null)
                return false;

            var result = _modelName.Trim();
            if (!InvalidOptions.IsNullOrEmpty() && InvalidOptions.Contains(result))
                return false;

            return (result.Length > 2 && result.Length <= 50)
            && (ValidationResults.Count > 0 && ValidationResults.All(x => x.IsValid));
        }

        private Task Cancel()
        {
            ModelSet = null;
            DialogResult = false;
            return Task.CompletedTask;
        }

        private bool CanExecuteCancel()
        {
            return true;
        }

        #region BaseWindow

        private Task WindowClose()
        {
            Close();
            return Task.CompletedTask;
        }

        private Task WindowRestore()
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
            return Task.CompletedTask;
        }

        private Task WindowMinimize()
        {
            WindowState = WindowState.Minimized;
            return Task.CompletedTask;
        }

        private Task WindowMaximize()
        {
            WindowState = WindowState.Maximized;
            return Task.CompletedTask;
        }

        private void OnContentRendered(object sender, EventArgs e)
        {
            InvalidateVisual();
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        #endregion
    }

    public record ValidationResult(OnnxModelType ModelType, bool IsValid);
}
