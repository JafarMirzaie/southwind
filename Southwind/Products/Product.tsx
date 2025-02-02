import * as React from 'react'
import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater, TypeContext, FormGroup, EntityTable } from '@framework/Lines'
import * as Finder from '@framework/Finder';
import { PredictorEntity } from '@extensions/Signum.MachineLearning/Signum.MachineLearning';
import SalesEstimation from './SalesEstimation';
import { ProductEntity } from './Southwind.Products';

export default function Product(p : { ctx: TypeContext<ProductEntity> }){
  const ctx = p.ctx;
  return (
    <div>
      <ValueLine ctx={ctx.subCtx(p => p.productName)} />
      <EntityCombo ctx={ctx.subCtx(p => p.supplier)} />
      <EntityLine ctx={ctx.subCtx(p => p.category)} />
      <ValueLine ctx={ctx.subCtx(p => p.quantityPerUnit)} />
      <ValueLine ctx={ctx.subCtx(p => p.unitPrice)} />
      <ValueLine ctx={ctx.subCtx(p => p.unitsInStock)} />
      {!ctx.value.isNew && <SalesEstimation ctx={ctx} />}
      <ValueLine ctx={ctx.subCtx(p => p.reorderLevel)} />
      <ValueLine ctx={ctx.subCtx(p => p.discontinued)} />
      <EntityTable ctx={ctx.subCtx(p => p.additionalInformation)} />
    </div>
  );
}

