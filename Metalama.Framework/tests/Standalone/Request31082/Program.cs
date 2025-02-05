using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Extensions.CodeFixes;
using Shox.Common.Logger;
using System.Security.Claims;
using System.Text.Json;

[assembly: AspectOrder(typeof(ToStringAttribute), typeof(LoggingAttribute))]
namespace Shox.Common.Logger;

public class LoggingAttribute : OverrideMethodAspect
{
	public override dynamic OverrideMethod()
	{
		try
		{
			var user = (meta.This.httpContextAccessor.HttpContext.User.Identity as ClaimsIdentity).Claims.FirstOrDefault(x => x.Type == "uid");
			var loggedInUserIdentity = !string.IsNullOrEmpty(user?.Value) ?
						               $" => by user: {user.Value}" :
						               string.Empty;
			
			var message = BuildInterpolatedString();
			message.AddExpression(loggedInUserIdentity);
			meta.This.logger.LogInformation(message.ToValue());

			var result = meta.Proceed();
			return result;
		}
		catch (Exception e)
		{
			var failureMessage = new InterpolatedStringBuilder();
			failureMessage.AddText(meta.Target.Method.Name);
			failureMessage.AddText(" failed: ");
			failureMessage.AddExpression(e.Message);
			meta.This.logger.LogInformation(failureMessage.ToValue());

			throw;
		}
	}

	protected InterpolatedStringBuilder BuildInterpolatedString()
	{
		var stringBuilder = new InterpolatedStringBuilder();

		stringBuilder.AddText($"{meta.Target.Type.Name}.{meta.Target.Method.Name}");

		stringBuilder.AddText("(");

		var i = 0;

		foreach (var prop in meta.Target.Parameters)
		{
			var comma = i > 0 ? ", " : "";

			if (i > 0)
				stringBuilder.AddText(", ");

			if (prop.Type.ToType() == typeof(string))
			{
				stringBuilder.AddText("'");
				stringBuilder.AddExpression(prop);
				stringBuilder.AddText("'");
			}
			else
			{
				stringBuilder.AddText(prop.Name);
				stringBuilder.AddText(" : ");
				stringBuilder.AddExpression(prop);
			}

			i++;
		}

		stringBuilder.AddText(")");

		return stringBuilder;
	}
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[CompileTime]
public class NotToStringAttribute : Attribute { }

[EditorExperience( SuggestAsLiveTemplate = true )]
public class ToStringAttribute : TypeAspect
{
	public override void BuildAspect(IAspectBuilder<INamedType> builder)
	{
		base.BuildAspect(builder);

		foreach (var field in builder.Target.FieldsAndProperties.Where(f => !f.IsStatic))
		{
			if (!field.Attributes.Any(a => a.Type.Is(typeof(NotToStringAttribute))))
			{
				builder.Diagnostics.Suggest(CodeFixFactory.AddAttribute(field, typeof(NotToStringAttribute), "Exclude from [ToString]"), field);
			}
		}

		if (builder.AspectInstance.Predecessors[0].Instance is IAttribute attribute)
		{
			builder.Diagnostics.Suggest(
				new CodeFix("Switch to manual implementation", codeFixBuilder => this.ImplementManually(codeFixBuilder, builder.Target)),
				attribute);
		}
	}

	[CompileTime]
	private async Task ImplementManually(ICodeActionBuilder builder, INamedType targetType)
	{
		await builder.ApplyAspectAsync(targetType, this);
		await builder.RemoveAttributesAsync(targetType, typeof(ToStringAttribute));
		await builder.RemoveAttributesAsync(targetType, typeof(NotToStringAttribute));
	}

	[Introduce(WhenExists = OverrideStrategy.Override, Name = "ToString")]
	public string IntroducedToString()
	{
		var stringBuilder = new InterpolatedStringBuilder();
		stringBuilder.AddText(meta.Target.Type.Name);
		stringBuilder.AddText(" ");

		var fields = meta.Target.Type.FieldsAndProperties.Where(f => !f.IsStatic).ToList();

		var i = meta.CompileTime(0);

		stringBuilder.AddText("{ ");
		foreach (var field in fields)
		{
			if (i > 0)
			{
				stringBuilder.AddText(", ");
			}

			stringBuilder.AddText(field.Name);
			stringBuilder.AddText(" : ");
			stringBuilder.AddExpression(JsonSerializer.Serialize(field.Value));

			i++;
		}
		stringBuilder.AddText(" }");

		return stringBuilder.ToValue();
	}
}