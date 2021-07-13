using ImageGallery.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGallery.API.Authorization
{
    public class MustOwnImageHandler : AuthorizationHandler<MustOwnImageReqirement>
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IGalleryRepository _galleryRepository;


        public MustOwnImageHandler(IHttpContextAccessor contextAccessor, IGalleryRepository galleryRepository)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _galleryRepository = galleryRepository ?? throw new ArgumentNullException(nameof(galleryRepository));
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MustOwnImageReqirement requirement)
        {
            var imageId = _contextAccessor.HttpContext.GetRouteValue("id").ToString();
            if(!Guid.TryParse(imageId, out Guid imageIdAsGuid))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var ownerId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if(!_galleryRepository.IsImageOwner(imageIdAsGuid, ownerId))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            context.Succeed(requirement);
            return Task.CompletedTask;

        }
    }
}
