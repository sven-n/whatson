﻿namespace Soloplan.WhatsON.Jenkins.GUI
{
  using System;
  using System.Windows.Controls;
  using System.Windows.Input;
  using Soloplan.WhatsON.GUI.SubjectTreeView;

  public class JenkinsProjectViewModel : SubjectViewModel
  {
    /// <summary>
    /// Backing field for <see cref="OpenWebPage"/>.
    /// </summary>
    private ICommand openWebPage;

    /// <summary>
    /// Command for opening builds webPage.
    /// </summary>
    public ICommand OpenWebPage => this.openWebPage ?? (this.openWebPage = new OpenWebPageCommand(this.Subject));

    public override void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
      base.OnDoubleClick(sender, e);
      var treeViewItem = sender as TreeViewItem;
      if (treeViewItem != null && treeViewItem.DataContext == this && this.OpenWebPage.CanExecute(null))
      {
        this.OpenWebPage.Execute(null);
      }
    }

    protected override StatusViewModel GetViewModelForStatus(Status status)
    {
      var viewModel = new JenkinsStatusViewModel(this);
      viewModel.Update(status);
      var jenkinsAddress = this.Subject as JenkinsProject;
      if (jenkinsAddress != null)
      {
        viewModel.SetJobAddress(jenkinsAddress.Address + "/job/" + jenkinsAddress.Project);
      }

      return viewModel;
    }

    private class OpenWebPageCommand : ICommand
    {
      private readonly JenkinsProject subject;

      public OpenWebPageCommand(Subject subject)
      {
        this.subject = subject as JenkinsProject;
      }

      public event EventHandler CanExecuteChanged;

      public bool CanExecute(object parameter)
      {
        return this.subject != null;
      }

      public void Execute(object parameter)
      {
        System.Diagnostics.Process.Start(this.subject.Address + "/job/" + this.subject.Project);
      }
    }
  }
}