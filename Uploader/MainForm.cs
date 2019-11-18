namespace Uploader
{

using System;
using System.Windows.Forms;
using Teensy.Net;
using Properties;

/// <summary>
/// The main (and only) form.
/// </summary>
public partial class MainForm : Form
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public MainForm() => InitializeComponent();

    /// <summary>
    /// Prompt user to select hex file to upload.
    /// </summary>
    private void ChooseHexFile(object    sender,
                               EventArgs e)
    {
        if ( _openFileDialog.ShowDialog(this) == DialogResult.OK )
        {
            try
            {
                HexImage = new HexImage(SelectedTeensy.TeensyType,
                                        _openFileDialog.FileName);
                SetUiState();
            }
            catch(Exception exception)
            {
                ShowException(exception);
            }
        }
    }

    /// <summary>
    /// Get the TeensyFactory object.
    /// </summary>
    private TeensyFactory Factory { get; set; }

    /// <summary>
    /// The HexImage to upload.
    /// </summary>
    private HexImage HexImage {get; set; }

    /// <summary>
    /// Override makes sure form is displayed before doing work.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        Show();
        Refresh();

        base.OnLoad(e);

        ShowWaitCursor( () =>
        {
            // Add all existing Teensys to listbox.
            Factory.EnumTeensies( teensy =>
            {
                TeensyAdded(teensy);
                return true;
            });
            
            // If a Teensy is selected, place focus on the file open button.
            if ( SelectedTeensy != null )
            {
                _fileButton.Focus();
            }

            // We want to be notified when Teensys are connected.
            Factory.TeensyAdded += TeensyAdded;

            SetUiState();
        });
    }

    /// <summary>
    /// Update the status and/or progress bar.
    /// </summary>
    public void ProvideFeedback(Teensy sender,
                                string status,
                                uint   bytesUploaded,
                                uint   uploadSize)
    {
        if ( InvokeRequired )
        {
            Invoke(new Action<Teensy,
                              string,
                              uint,
                              uint>(ProvideFeedback),
                   sender,
                   status,
                   bytesUploaded,
                   uploadSize);
        }
        else
        {
            _status.Text = status;

            if ( bytesUploaded > 0 )
            {
                _progress.Maximum = (int)uploadSize;
                _progress.Value =   (int)bytesUploaded;
            }

            Refresh();
        }
    }

    /// <summary>
    /// Reboot the Teensy.
    /// </summary>
    private void Reboot(object    sender,
                        EventArgs e) => ShowWaitCursor(SelectedTeensy.Reboot);

    /// <summary>
    /// Get the selected Teensy, if any.
    /// </summary>
    private Teensy SelectedTeensy =>
        _teensys.SelectedIndex > -1
        ? (Teensy)_teensys.Items[_teensys.SelectedIndex]
        : null;

    /// <summary>
    /// Set controls based on current state.
    /// </summary>
    private void SetUiState()
    {
        var selected = SelectedTeensy;

        _fileButton.Enabled = selected != null;

        _rebootButton.Enabled = selected != null &&
                                selected.UsbType != UsbTypes.Disconnected;

        // Make sure that not only is a Teensy selected, but it matches the
        // type used when HexImage was created. If not, the user must reselect
        // it.
        _uploadButton.Enabled =
            _rebootButton.Enabled &&
            HexImage != null      &&
            HexImage.TeensyType == selected?.TeensyType;
    }

    /// <summary>
    /// Set UI state from GUI interactions.
    /// </summary>
    private void SetUiState(object    sender,
                            EventArgs e) => SetUiState();

    /// <summary>
    /// Show exception to use.
    /// </summary>
    private void ShowException(Exception exception)
    {
        MessageBox.Show(this,
                        exception.Message,
                        Resources.Error,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
    }

    /// <summary>
    /// Show the wait cursor while a callback operation is run. This will also
    /// create the Factory object, as needed. If the callback results in an
    /// exception being thrown, it will be shown to the user.
    /// </summary>
    public void ShowWaitCursor(Action callback)
    {
        var old = Cursor;
        Cursor =  Cursors.WaitCursor;

        if ( Factory == null )
        {
            Factory = new TeensyFactory();
        }

        try
        {
            callback();
        }
        catch(Exception e)
        {
            Cursor = old;
            ShowException(e);
        }

        Cursor = old;
    }

    /// <summary>
    /// Notification of a Teensy added to the system.
    /// </summary>
    private void TeensyAdded(Teensy teensy)
    {
        // This may be called from a background thread. Deal with this.
        if ( InvokeRequired )
        {
            Invoke(new Action<Teensy>(TeensyAdded), teensy);
        }
        else
        {
            // Add to listbox.
            _teensys.Items.Add(teensy);

            // Hook into events we care about.
            teensy.FeedbackProvided       += ProvideFeedback;
            teensy.ConnectionStateChanged += TeensyConnectionStateChanged;

            if ( SelectedTeensy == null )
            {
                _teensys.SelectedItem = teensy;
            }

            SetUiState();
        }
    }

    /// <summary>
    /// Notification that a Teensy connection state changed.
    /// </summary>
    private void TeensyConnectionStateChanged(Teensy teensy)
    {
        // This may be called from a background thread. Deal with this.
        if ( InvokeRequired )
        {
            Invoke(new Action<Teensy>(TeensyConnectionStateChanged),
                   teensy);
        }
        else
        {
            // Refresh the listbox.
            var index =    _teensys.Items.IndexOf(teensy);
            var selected = _teensys.SelectedItem;

            _teensys.Items.RemoveAt(index);
            _teensys.Items.Insert(index, teensy);

            if ( selected != null )
            {
                _teensys.SelectedItem = selected;
            }

            SetUiState();
        }
    }

    /// <summary>
    /// Start the upload.
    /// </summary>
    private void Upload(object    sender,
                        EventArgs e)
    {
        // Try to check that the image is valid for the selected
        // Teensy type. This is not perfect, so allow the user to choose to
        // attempt upload anyway if uncertain, but allow them to cancel
        // it as well.
        if ( HexImage.IsKnownGood ||
             MessageBox.Show(
                this,
                string.Format(Resources.
                    CannotValidateTeensyFormat,
                    SelectedTeensy.Name),
                Resources.PossibleCorruption,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes )
        {
            ShowWaitCursor( () =>
            {
                SelectedTeensy.UploadImage(HexImage);
            });
        }
    }
}

}
