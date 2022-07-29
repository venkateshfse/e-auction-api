﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using EAuction.Bid.WebApi.Exceptions;
using EAuction.Order.Application.Commands;
using EAuction.Order.Application.Commands.OrderCreate;
using EAuction.Order.Application.Queries;
using EAuction.Order.Application.Responses;
using EAuction.Order.Domain.Entities;
using EAuction.Order.Domain.Repositories;
using EAuction.Products.Api.Repositories.Abstractions;
using EventBusRabbitMQ.Core;
using EventBusRabbitMQ.Events;
using EventBusRabbitMQ.Producer;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EAuction.Order.WebApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class BidController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<BidController> _logger;
        private readonly IMapper _mapper;   
        private readonly IBidRepository _bidRepository;
        private readonly IProductRepository _productRepository;


        public BidController(IMediator mediator,
            ILogger<BidController> logger,
            IMapper mapper,
              IBidRepository bidRepository,
            IProductRepository productRepository)
        {
            _mediator = mediator;
            _logger = logger;
            _mapper = mapper;
            _bidRepository = bidRepository;
            _productRepository = productRepository;
        }

        [HttpGet("GetBids/{productId}")]
        [ProducesResponseType(typeof(IEnumerable<BidResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<IEnumerable<BidResponse>>> GetBidsByProductId(string productId)
        {
            var query = new GetBidsByProductIdQuery(productId);

            var bids = await _mediator.Send(query);
            if (bids.Count() == 0)
            {
                return NotFound();
            }

            return Ok(bids);
        }

        [HttpGet("GetBid/{productId}/{buyerEmailId}")]
        [ProducesResponseType(typeof(IEnumerable<BidResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<IEnumerable<BidResponse>>> GetBidByProductIdAndEmailId(string productId, string buyerEmailId)
        {
            var query = new GetBidsByProductIdAndEmailIdQuery(productId, buyerEmailId);
            var bid = await _mediator.Send(query);

            if (bid == null)
            {
                return NotFound();
            }

            return Ok(bid);
        }

        [HttpPost("PlaceBid")]
        [ProducesResponseType(typeof(EAuction.Order.Domain.Entities.Bid), (int)HttpStatusCode.Created)]
        public async Task<ActionResult<BidResponse>> BidCreate([FromBody] BidCreateCommand bidCreateCommand)
        {
            var result = await _mediator.Send(bidCreateCommand);
            BidCreateEvent eventMessage = _mapper.Map<BidCreateEvent>(bidCreateCommand);

            try
            {
                //await _eventBus.SendMessage(eventMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR Publishing integration event: {EventId} from {AppName}", eventMessage.Id, "Bidding");
                throw;
            }

            return Ok(result);
        }

        [HttpPut("UpdateBid/{productId}/{buyerEmailId}/{newBidAmount}")]
        [ProducesResponseType(typeof(EAuction.Order.Domain.Entities.Bid), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateBidAmount(string productId, string buyerEmailId, decimal newBidAmount)
        {
            var product = await _productRepository.GetProduct(productId);
            if (product.BidEndDate < DateTime.Today)
            {
                throw new BidExpiredException("Bid cannot be updated after the bid end date");
            }

            var updateCommand = new BidUpdateCommand(productId, buyerEmailId, newBidAmount);

            return Ok(await _mediator.Send(updateCommand));
        }
               
    }
}
