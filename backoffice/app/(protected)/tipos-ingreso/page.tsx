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
>("/api/tipos-ingreso");

export default function Page() {
  return (
    <CatalogoConceptoPage
      title="Tipos de Ingreso"
      description=""
      resource={resource}
    />
  );
}
