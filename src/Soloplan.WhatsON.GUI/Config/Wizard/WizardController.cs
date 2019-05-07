﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WizardController.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.Wizard
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Controls;
  using MaterialDesignThemes.Wpf;

  /// <summary>
  /// Controls the execution of a wizard which allows to create or edit a project connection.
  /// </summary>
  /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
  public class WizardController : INotifyPropertyChanged
  {
    /// <summary>
    /// The owner window.
    /// </summary>
    private readonly Window ownerWindow;

    /// <summary>
    /// The wizard window.
    /// </summary>
    private WizardWindow wizardWindow;

    /// <summary>
    /// The current page.
    /// </summary>
    private Page currentPage;

    /// <summary>
    /// The projects view model.
    /// </summary>
    private ProjectViewModelList projects;

    /// <summary>
    /// Is any project checked.
    /// </summary>
    private bool isAnyProjectChecked;

    /// <summary>
    /// Is finish enabled.
    /// </summary>
    private bool isFinishEnabled;

    /// <summary>
    /// The subject plugin.
    /// </summary>
    private ISubjectPlugin subjectPlugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="WizardController"/> class.
    /// </summary>
    /// <param name="ownerWindow">The owner window.</param>
    public WizardController(Window ownerWindow)
    {
      this.ownerWindow = ownerWindow;
    }

    /// <summary>
    /// Occurs when property was changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets the projects tree.
    /// </summary>
    public ProjectViewModelList Projects => this.projects ?? (this.projects = new ProjectViewModelList());

    /// <summary>
    /// Gets a value indicating whether wizard is NOT on it's first step.
    /// </summary>
    public bool IsNotFirstStep => !(this.currentPage is ConnectionWizardPage);

    /// <summary>
    /// Gets a value indicating whether wizard is on it's last step.
    /// </summary>
    public bool IsLastStep => this.currentPage is ProjectSelectionWizardPage;

    /// <summary>
    /// Gets a value indicating whether wizard is NOT on it's last step.
    /// </summary>
    public bool IsNotLastStep => !this.IsLastStep;

    /// <summary>
    /// Gets or sets a value indicating whether multi selection mode is active.
    /// </summary>
    public bool MultiSelectionMode { get; set; }

    /// <summary>
    /// Gets or sets the proposed server address.
    /// </summary>
    public string ProposedServerAddress { get; set; }

    /// <summary>
    /// Gets a value indicating whether any project is checked.
    /// </summary>
    public bool IsAnyProjectChecked
    {
      get => this.isAnyProjectChecked;
      private set
      {
        this.isAnyProjectChecked = value;
        this.OnPropertyChanged();
      }
    }

    public bool IsFinishEnabled
    {
      get => this.isFinishEnabled;
      private set
      {
        this.isFinishEnabled = value;
        this.OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets the wizard frame.
    /// </summary>
    private Frame WizardFrame => this.wizardWindow?.Frame;

    /// <summary>
    /// Gets the step description text block control.
    /// </summary>
    private TextBlock StepDescriptionTextBlock => this.wizardWindow.StepDescription;

    /// <summary>
    /// Starts the wizard.
    /// </summary>
    /// <param name="plugin">The subject plugin.</param>
    public bool Start(ISubjectPlugin plugin = null)
    {
      this.subjectPlugin = plugin;
      this.wizardWindow = new WizardWindow(this);
      this.wizardWindow.Owner = this.ownerWindow;
      this.GoToConnectionStep();
      if (this.wizardWindow.ShowDialog() == true)
      {
        return true;
      }

      return false;
    }

    /// <summary>
    /// Retrieves selected projects.
    /// </summary>
    /// <returns>The selected projects.</returns>
    public IList<ServerProject> GetSelectedProjects()
    {
      if (this.Projects == null || this.Projects.Count == 0)
      {
        return new List<ServerProject>();
      }

      var serverProjects = new List<ServerProject>();
      var checkedProjects = this.Projects.GetChecked();
      foreach (var checkedProject in checkedProjects)
      {
        var newServerProject = new ServerProject();
        newServerProject.Address = checkedProject.Address;
        newServerProject.Name = checkedProject.Name;
        serverProjects.Add(newServerProject);
      }

      return serverProjects;
    }

    /// <summary>
    /// Goes to next page of the wizard.
    /// </summary>
    public void GoToNextPage()
    {
      if (this.WizardFrame.Content is ConnectionWizardPage)
      {
        this.GoToProjectSelectionStep();
      }
    }

    /// <summary>
    /// Goes to previous page of the wizard.
    /// </summary>
    public void GoToPrevPage()
    {
      if (this.WizardFrame.Content is ProjectSelectionWizardPage)
      {
        this.GoToConnectionStep();
      }
    }

    /// <summary>
    /// Finishes this wizard.
    /// </summary>
    public void Finish()
    {
      this.wizardWindow.DialogResult = true;
      this.wizardWindow.Close();
    }

    /// <summary>
    /// Called when property was changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Called when page was changed.
    /// </summary>
    private void OnPageChanged()
    {
      this.OnPropertyChanged(nameof(this.IsNotFirstStep));
      this.OnPropertyChanged(nameof(this.IsLastStep));
      this.OnPropertyChanged(nameof(this.IsNotLastStep));
      this.OnPropertyChanged(nameof(this.IsFinishEnabled));
    }

    /// <summary>
    /// Goes to connection step of the wizard.
    /// </summary>
    private void GoToConnectionStep()
    {
      this.StepDescriptionTextBlock.Text = "Connection"; // TODO load from resources
      this.currentPage = new ConnectionWizardPage();
      this.currentPage.DataContext = this;
      this.WizardFrame.Content = this.currentPage;
      this.OnPageChanged();
    }

    private void ProcessServerSubProjects(IList<ServerProjectTreeItem> serverProjects, ProjectViewModel projectViewModel)
    {
      foreach (var serverSubProject in serverProjects)
      {
        var newProject = projectViewModel.AddProject(serverSubProject.Name);
        newProject.Address = serverSubProject.Address;
        this.ProcessServerSubProjects(serverSubProject.ServerProjects, newProject);
      }
    }

    /// <summary>
    /// Prepares the projects list.
    /// </summary>
    /// <returns>The task.</returns>
    private async Task PrepareProjectsList()
    {
      // TODO first,try to detect pluginType if it's not set

      this.Projects.MultiSelectionMode = false;
      var listQueringPlugin = (IProjectsListQuerying)this.subjectPlugin;
      var serverProjects = await listQueringPlugin.GetProjectsLists(this.ProposedServerAddress);
      foreach (var serverProject in serverProjects)
      {
        var newProject = this.Projects.AddProject(serverProject.Name);
        newProject.Address = serverProject.Address;
        this.ProcessServerSubProjects(serverProject.ServerProjects, newProject);
      }

      var projectCheckedChangedAction = new Action(() => this.IsAnyProjectChecked = this.Projects.Any(p => p.IsAnyChecked()));
      foreach (var project in this.Projects)
      {
        this.AttachToProjectPropertyChanged(project, projectCheckedChangedAction);
      }
    }

    /// <summary>
    /// Attaches action to each project PropertyChanged event.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="action">The action.</param>
    private void AttachToProjectPropertyChanged(ProjectViewModel project, Action action)
    {
      project.PropertyChanged += (s, e) => action();
      foreach (var subProject in project.Projects)
      {
        this.AttachToProjectPropertyChanged(subProject, action);
      }
    }

    /// <summary>
    /// Goes to project selection step of the wizard.
    /// </summary>
    private async void GoToProjectSelectionStep()
    {
      var error = false;
      var errorMessage = string.Empty;
      var waitDailogTask = DialogHost.Show(this.wizardWindow.WizardWaitDialogHost.DialogContent, "WizardWaitDialogHostId");
      try
      {
        await this.PrepareProjectsList();
      }
      catch (Exception e)
      {
        error = true;
        errorMessage = $"There was a connection error,{Environment.NewLine}details: {e.Message}"; // TODO load from resources
      }

      DialogHost.CloseDialogCommand.Execute("WizardWaitDialogHostId", this.wizardWindow.WizardWaitDialogHost);

      if (!error)
      {
        this.StepDescriptionTextBlock.Text = "Project selection"; // TODO load from resources
        this.currentPage = new ProjectSelectionWizardPage();
        this.currentPage.DataContext = this;
        this.WizardFrame.Content = this.currentPage;
        this.OnPageChanged();
      }
      else
      {
        System.Windows.MessageBox.Show(this.wizardWindow, errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}