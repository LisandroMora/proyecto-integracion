"use client";

import {
  CatalogoConceptoPage,
  type CatalogoConceptoCreate,
  type CatalogoConceptoRead,
  type CatalogoConceptoUpdate,
} from "@/components/CatalogoConceptoPage";
import { createResourceClient } from "@/lib/resource";

const resource = createResourceClient<
  CatalogoConceptoRead,
  CatalogoConceptoCreate,
  CatalogoConceptoUpdate
>("/api/tipos-deduccion");

export default function Page() {
  return (
    <CatalogoConceptoPage
      title="Tipos de Deducción"
      description=""
      resource={resource}
    />
  );
}
