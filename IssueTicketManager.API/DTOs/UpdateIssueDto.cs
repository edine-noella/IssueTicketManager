using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using IssueTicketManager.API.Models;

namespace IssueTicketManager.API.DTOs;

public class UpdateIssueDto
{
   [MaxLength(200)]
   public string? Title { get; set; }
   public string? Body { get; set; }
   
   [JsonConverter(typeof(JsonStringEnumConverter))]
   public IssueStatus? Status { get; set; }
   public int? AssigneeId { get; set; }
   public List<int>? LabelIds { get; set; }
}