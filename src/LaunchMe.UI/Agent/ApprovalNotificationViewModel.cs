using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.Contracts.Agent;
using LaunchMe.UI.ViewModels;

namespace LaunchMe.UI.Agent;

/// <summary>
/// ViewModel coordinating pending agent permission approval requests, diff previews, and decisions.
/// </summary>
public class ApprovalNotificationViewModel : INotifyPropertyChanged
{
    private readonly IAgentApprovalService _approvalService;
    private readonly IDiffParserService _diffParser;

    private AgentApprovalRequest? _activeRequest;
    private DiffDocument? _parsedDiff;
    private bool _isDiffExpanded;

    public ApprovalNotificationViewModel(IAgentApprovalService approvalService, IDiffParserService diffParser)
    {
        _approvalService = approvalService;
        _diffParser = diffParser;

        _approvalService.ApprovalRequested += OnApprovalRequested;
        _approvalService.ApprovalResolved += OnApprovalResolved;

        ApproveCommand = new RelayCommand(async () =>
        {
            if (_activeRequest != null)
            {
                await _approvalService.SubmitDecisionAsync(_activeRequest.RequestId, ApprovalAction.Allowed);
            }
        });

        DenyCommand = new RelayCommand(async () =>
        {
            if (_activeRequest != null)
            {
                await _approvalService.SubmitDecisionAsync(_activeRequest.RequestId, ApprovalAction.Denied);
            }
        });

        RememberRuleCommand = new RelayCommand(async () =>
        {
            if (_activeRequest != null)
            {
                await _approvalService.SubmitDecisionAsync(_activeRequest.RequestId, ApprovalAction.RememberRule);
            }
        });

        ToggleDiffCommand = new RelayCommand(() =>
        {
            IsDiffExpanded = !IsDiffExpanded;
        });

        // Initialize with first pending request if any
        _activeRequest = _approvalService.GetPendingRequests().FirstOrDefault();
        UpdateDiff();
    }

    public bool HasPendingApproval => _activeRequest != null;

    public Microsoft.UI.Xaml.Visibility ApprovalCardVisibility =>
        HasPendingApproval ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public AgentApprovalRequest? ActiveRequest
    {
        get => _activeRequest;
        private set
        {
            if (_activeRequest != value)
            {
                _activeRequest = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasPendingApproval));
                OnPropertyChanged(nameof(ApprovalCardVisibility));
                UpdateDiff();
            }
        }
    }

    public DiffDocument? ParsedDiff
    {
        get => _parsedDiff;
        private set
        {
            if (_parsedDiff != value)
            {
                _parsedDiff = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasDiff));
            }
        }
    }

    public bool HasDiff => _parsedDiff != null && _parsedDiff.Files.Count > 0;

    public bool IsDiffExpanded
    {
        get => _isDiffExpanded;
        set
        {
            if (_isDiffExpanded != value)
            {
                _isDiffExpanded = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand ApproveCommand { get; }
    public ICommand DenyCommand { get; }
    public ICommand RememberRuleCommand { get; }
    public ICommand ToggleDiffCommand { get; }

    public async Task SelectOptionAsync(string option)
    {
        if (_activeRequest != null)
        {
            await _approvalService.SubmitDecisionAsync(_activeRequest.RequestId, ApprovalAction.Allowed, userResponseText: option);
        }
    }

    private void OnApprovalRequested(object? sender, AgentApprovalRequest request)
    {
        ActiveRequest = request;
    }

    private void OnApprovalResolved(object? sender, string requestId)
    {
        if (_activeRequest?.RequestId == requestId)
        {
            ActiveRequest = _approvalService.GetPendingRequests().FirstOrDefault();
        }
    }

    private void UpdateDiff()
    {
        if (_activeRequest != null && !string.IsNullOrWhiteSpace(_activeRequest.FileDiffUnified))
        {
            ParsedDiff = _diffParser.Parse(_activeRequest.FileDiffUnified);
        }
        else
        {
            ParsedDiff = null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
