using ApiWorker.Documents.DTOs;
using ApiWorker.Documents.Interfaces;
using ApiWorker.Documents.Services;
using ApiWorker.Documents.ValueObjects;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiWorker.Storage;

namespace ApiWorker.Documents.Controllers;

/// <summary>
/// API controller for managing document templates.
/// Provides endpoints to list and retrieve global templates.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;
    private readonly IMapper _mapper;
    private readonly IBlobStorageService _blobStorage;
    private readonly IValidator<ListTemplatesRequest> _listValidator;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        ITemplateService templateService,
        IMapper mapper,
        IBlobStorageService blobStorage,
        IValidator<ListTemplatesRequest> listValidator,
        ILogger<TemplatesController> logger)
    {
        _templateService = templateService;
        _mapper = mapper;
        _blobStorage = blobStorage;
        _listValidator = listValidator;
        _logger = logger;
    }

    /// <summary>List all global templates, optionally filtered by document type</summary>
    [HttpGet]
    public async Task<ActionResult<ListTemplatesResponse>> ListTemplates([FromQuery] ListTemplatesRequest request, CancellationToken ct)
    {
        if (request == null)
            request = new ListTemplatesRequest();

        var validation = await _listValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errorMessages = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ListTemplatesResponse
            {
                Success = false,
                Message = errorMessages.Count == 1
                    ? errorMessages.First()
                    : $"Please fix the following errors: {string.Join("; ", errorMessages)}"
            });
        }

        try
        {
            // Use empty GUID for businessId since we only return global templates
            var templates = await _templateService.ListTemplatesAsync(Guid.Empty, request.Type, ct);

            var templateDtos = templates.Select(t => MapTemplateToDto(t)).ToList();

            return Ok(new ListTemplatesResponse
            {
                Success = true,
                Message = $"Found {templateDtos.Count} template(s)",
                Templates = templateDtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list templates");
            return StatusCode(500, new ListTemplatesResponse
            {
                Success = false,
                Message = "Unable to retrieve templates. Please try again later."
            });
        }
    }

    /// <summary>Get template details by ID</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GetTemplateResponse>> GetTemplate(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
            return BadRequest(new GetTemplateResponse
            {
                Success = false,
                Message = "Invalid template ID. Please provide a valid template ID."
            });

        try
        {
            var template = await _templateService.GetTemplateAsync(id, ct);

            if (template == null)
                return NotFound(new GetTemplateResponse
                {
                    Success = false,
                    Message = "Template not found. The template may have been deleted or the ID is incorrect."
                });

            var templateDto = MapTemplateToDto(template);

            return Ok(new GetTemplateResponse
            {
                Success = true,
                Message = "Template retrieved successfully",
                Template = templateDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template {TemplateId}", id);
            return StatusCode(500, new GetTemplateResponse
            {
                Success = false,
                Message = "Unable to retrieve template. Please try again later."
            });
        }
    }

    private TemplateDto MapTemplateToDto(ApiWorker.Documents.Entities.Template template)
    {
        var blobUrl = BuildBlobUrl(template.BlobPath);

        return new TemplateDto
        {
            Id = template.Id,
            Type = template.Type,
            Name = template.Name,
            Version = template.Version,
            BlobUrl = blobUrl,
            PreviewUrl = template.PreviewBlobUrl,
            Theme = string.IsNullOrWhiteSpace(template.ThemeJson)
                ? null
                : DocumentTheme.FromJson(template.ThemeJson).ToDto(),
            IsDefault = template.IsDefault,
            CreatedAt = template.CreatedAt
        };
    }

    private string BuildBlobUrl(string blobPath)
    {
        // Construct full blob URL from path
        // The blob storage service should provide a method to get the URL, but for now we'll construct it
        // In production, use the blob storage service to get the actual URL
        // For templates, we need to download from blob storage using the path
        return blobPath; // This will be used by the blob storage service to download
    }
}

