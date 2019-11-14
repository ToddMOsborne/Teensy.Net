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
            HexImage = new HexImage(SelectedTeensy,
                                    _openFileDialog.FileName);

            if ( !HexImage.IsValid )
            {
                HexImage = null;
            }

            SetUiState();
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
    /// Notification of exception. Display to user.
    /// </summary>
    private void LastExceptionChanged(TeensyFactory sender)
    {
        if ( InvokeRequired )
        {
             Invoke(new Action<TeensyFactory>(LastExceptionChanged), sender);
        }
        else
        {
            MessageBox.Show(this,
                            Factory.LastException.Message,
                            Resources.Error,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

            // Clear now that user has been notified.
            Factory.LastException = null;
        }
    }

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
            // If exceptions occur, we want to be notified.
            Factory.LastExceptionChanged += LastExceptionChanged;

            // Add all existing Teensys to listbox.
            Factory.EnumTeensys( teensy =>
            {
                TeensyAdded(teensy);
                return true;
            });
            
            // Select first Teensy?
            if ( _teensys.Items.Count > 0 )
            {
                _teensys.SelectedIndex = 0;
            }

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
            _status.Refresh();

            if ( bytesUploaded > 0 )
            {
                _progress.Maximum = (int)uploadSize;
                _progress.Value =   (int)bytesUploaded;
                _progress.Refresh();
            }
        }
    }

    /// <summary>
    /// Reboot the Teensy.
    /// </summary>
    private void Reboot(object    sender,
                        EventArgs e)
    {
        ShowWaitCursor( () =>
        {
            if ( !SelectedTeensy.Reboot() )
            {
                // If not already set, create exception to show message to
                // user.
                if ( Factory.LastException == null )
                {
                    Factory.LastException =
                        new Exception(Resources.RebootFailed);
                }
            }
        });
    }

    /// <summary>
    /// Get the selected Teensy, if any.
    /// </summary>
    private Teensy SelectedTeensy => _teensys.SelectedIndex > -1
                                     ? (Teensy)_teensys.Items[_teensys.SelectedIndex]
                                     : null;

    /// <summary>
    /// Set controls based on current state.
    /// </summary>
    private void SetUiState()
    {
        _rebootButton.Enabled =
            SelectedTeensy != null &&
            SelectedTeensy.UsbType != UsbTypes.Disconnected;

        // Make sure that not only is a Teensy selected, but it matches the one
        // used when HexImage was created. If not, the user must reselect it,
        // even if the same model.
        _uploadButton.Enabled =
            _rebootButton.Enabled &&
            HexImage != null      &&
            HexImage.Teensy.SerialNumber ==
                // ReSharper disable once PossibleNullReferenceException
                SelectedTeensy.SerialNumber;
    }

    /// <summary>
    /// Set UI state from GUI interactions.
    /// </summary>
    private void SetUiState(object    sender,
                            EventArgs e) => SetUiState();

    /// <summary>
    /// Show the wait cursor while a callback operation is run. If interuptable
    /// is true, a wait cursor with arrow will be shown, allowing an operation
    /// to be cancelled. This will also create the Factory object, as needed,
    /// and clear the LastException.
    /// </summary>
    public void ShowWaitCursor(Action  callback,
                               bool    interuptable = false)
    {
        var old = Cursor;

        Cursor =  interuptable ? Cursors.AppStarting : Cursors.WaitCursor;

        if ( Factory == null )
        {
            Factory = new TeensyFactory();
        }

        Factory.LastException = null;
        Factory.SafeMethod(callback);

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
            var selected = _teensys.SelectedIndex;

            _teensys.Items.RemoveAt(index);
            _teensys.Items.Insert(index, teensy);

            if ( selected == index )
            {
                _teensys.SelectedIndex = selected;
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
        ShowWaitCursor( () =>
        {
            // Try to check that the image is valid for the selected
            // Teensy. This is not perfect, so allow the user to choose to
            // attempt upload anyway if uncertain, but allow them to cancel
            // it as well.
            if ( HexImage.IsValidForTeensy ||
                 MessageBox.Show(
                    this,
                    string.Format(Resources.
                        CannotValidateTeensyFormat,
                        SelectedTeensy.Name),
                    Resources.PossibleCorruption,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes )
            {
                MessageBox.Show(
                    this,
                    SelectedTeensy.UploadImage(HexImage).ToString(),
                    Resources.UploadComplete);
            }
        });
    }
}

}
