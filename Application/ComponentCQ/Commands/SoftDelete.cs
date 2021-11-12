using Application.Interfaces;
using AutoMapper;
using FluentValidation;
using MediatR;
using Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Application.Exceptions;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace Application.ComponentCQ.Commands
{
    public class SoftDelete
    {
        public class Command : IRequest<bool>
        {
            public Guid Id { get; set; }
            public class Validator : AbstractValidator<Command>
            {
                public Validator()
                {
                    RuleFor(x => x.Id).NotEmpty();
                }
            }
            public class Handler : IRequestHandler<Command, bool>
            {
                DataContext db;
                iUserAccessor userAccessor;
                public Handler(DataContext dataContext,
                               iUserAccessor userAccessor)
                {
                    this.db = dataContext;
                    this.userAccessor = userAccessor;
                }

                public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
                {
                    var userId = userAccessor.GetId();
                    Component component = db.Components.FirstOrDefault(x => x.Id == request.Id);

                    if (component == null)
                        throw new RestException(HttpStatusCode.NotFound, new { Component = "Not found" });
                    if (userId != component.UserId || component.LibraryId == null)
                        throw new RestException(HttpStatusCode.NotFound, new { Component = "Denied" });

                    component.Deleted = ! component.Deleted;
                    await db.SaveChangesAsync();
                    return component.Deleted;
                }
            }
        }
    }
}
