namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Diagnostics.Contracts;
    using System.Windows.Controls;
    using System.Windows.Interactivity;

    using JetBrains.Annotations;

    using tomenglertde.ProjectConfigurationManager.Model;

    public sealed class DataGridTryBeginEditBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            Contract.Assume(AssociatedObject != null);

            AssociatedObject.BeginningEdit += DataGrid_BeginningEdit;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            Contract.Assume(AssociatedObject != null);

            AssociatedObject.BeginningEdit -= DataGrid_BeginningEdit;
        }

        private static void DataGrid_BeginningEdit([NotNull] object sender, [NotNull] DataGridBeginningEditEventArgs e)
        {
            var dataGridRow = e.Row;
            var configuration = dataGridRow?.Item as ProjectConfiguration;
            var project = configuration?.Project;

            if (project == null || !project.CanEdit())
            {
                e.Cancel = true;
            }
        }
    }
}
