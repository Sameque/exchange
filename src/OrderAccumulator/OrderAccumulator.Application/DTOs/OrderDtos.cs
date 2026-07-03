namespace OrderAccumulator.Application.DTOs;

public record OrderRequest(string Symbol, decimal Quantity, decimal Price, string Side);
public record OrderResponse(bool Accepted, string Message);
public record SymbolResponse(string Symbol, string Description);
public record ExposureResponse(string Symbol, decimal ValueExposed);
