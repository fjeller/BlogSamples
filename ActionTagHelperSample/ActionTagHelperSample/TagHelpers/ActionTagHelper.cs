using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ActionTagHelperSample.TagHelpers
{
	public class ActionTagHelper : TagHelper
	{

		#region constants

		private const string _ATTRIBUTENAME_ACTION = "asp-action";
		private const string _ATTRIBUTENAME_CONTROLLER = "asp-controller";
		private const string _ATTRIBUTENAME_AREA = "asp-area";
		private const string _ROUTEVALUESDICTIONARY_NAME = "asp-all-route-data";
		private const string _ROUTEVALUESDICTIONARY_PREFIX = "asp-route-";

		#endregion

		#region fields

		private IDictionary<string, string> _routeValues = null;
		private readonly IUrlHelperFactory _urlHelperFactory;

		#endregion

		#region properties

		[ViewContext]
		public ViewContext ViewContext { get; set; }

		/// <summary>
		/// The name of the action method.
		/// </summary>
		[HtmlAttributeName( _ATTRIBUTENAME_ACTION )]
		public string Action { get; set; }

		/// <summary>
		/// The name of the controller.
		/// </summary>
		[HtmlAttributeName( _ATTRIBUTENAME_CONTROLLER )]
		public string Controller { get; set; }

		/// <summary>
		/// The name of the area.
		/// </summary>
		[HtmlAttributeName( _ATTRIBUTENAME_AREA )]
		public string Area { get; set; }

		/// <summary>
		/// Additional parameters for the route.
		/// </summary>
		[HtmlAttributeName( _ROUTEVALUESDICTIONARY_NAME, DictionaryAttributePrefix = _ROUTEVALUESDICTIONARY_PREFIX )]
		public IDictionary<string, string> RouteValues
		{
			get { return _routeValues ??= new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase ); }
			set { _routeValues = value; }
		}

		#endregion

		#region Processing

		public override async Task ProcessAsync( TagHelperContext context, TagHelperOutput output )
		{
			var routeValues = RouteValues.ToDictionary(
				kvp => kvp.Key,
				kvp => (object)kvp.Value,
				StringComparer.OrdinalIgnoreCase );

			if ( !String.IsNullOrEmpty( Area ) )
			{
				routeValues.Add( "Area", Area );
			}

			IUrlHelper helper = _urlHelperFactory.GetUrlHelper( ViewContext );

			string finalUrl;
			if ( String.IsNullOrEmpty( Controller ) )
			{
				finalUrl = helper.Action( Action, routeValues );
			}
			else
			{
				finalUrl = helper.Action( Action, Controller, routeValues );
			}

			Uri baseAddress = new Uri( $"{ViewContext.HttpContext.Request.Scheme}://{ViewContext.HttpContext.Request.Host}" );
			var cookieContainer = new CookieContainer();
			foreach ( var cookie in ViewContext.HttpContext.Request.Cookies )
			{
				cookieContainer.Add( baseAddress, new Cookie( cookie.Key, cookie.Value ) );
			}

			using ( HttpClientHandler handler = new HttpClientHandler() { CookieContainer = cookieContainer } )
			{
				using ( HttpClient client = new HttpClient( handler ) )
				{
					client.BaseAddress = baseAddress;
					var result = await client.GetAsync( finalUrl );
					output.Content.SetHtmlContent( await result.Content.ReadAsStringAsync() );
				}
			}
		}

		#endregion

		#region Constructor

		public ActionTagHelper( IUrlHelperFactory urlHelperFactory )
		{
			this._urlHelperFactory = urlHelperFactory;
		}

		#endregion

	}
}
