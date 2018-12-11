<?xml version="1.0" encoding="utf-8"?>
<!--
 Copyright 2018 Max Toro Q.

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->
<stylesheet version="2.0" exclude-result-prefixes="#all"
   xmlns="http://www.w3.org/1999/XSL/Transform"
   xmlns:xs="http://www.w3.org/2001/XMLSchema"
   xmlns:c="http://maxtoroq.github.io/XCST"
   xmlns:xcst="http://maxtoroq.github.io/XCST/syntax"
   xmlns:src="http://maxtoroq.github.io/XCST/compiled"
   xmlns:wqt="http://maxtoroq.github.io/WebQueryToolkit">

   <template match="c:type | c:member" mode="src:type-attribute-extra">
      <next-match/>
      <if test="@wqt:queryable[xcst:boolean(.)]">
         <call-template name="wqt:web-queryable-attribute"/>
      </if>
   </template>

   <template name="wqt:web-queryable-attribute">
      <param name="modules" tunnel="yes"/>

      <variable name="defs" select="for $m in reverse($modules) return $m/wqt:web-query"/>
      <variable name="setters" as="text()*">
         <apply-templates select="@wqt:order-by" mode="wqt:setter"/>
         <apply-templates select="(for $d in $defs return $d/@top)[1]" mode="wqt:setter"/>
         <apply-templates select="(for $d in $defs return $d/@top-max)[1]" mode="wqt:setter"/>
         <call-template name="wqt:array-setter">
            <with-param name="name" select="'FilterAllowedProperties'"/>
            <with-param name="values" select="c:member[@wqt:filterable[xcst:boolean(.)]]/xcst:name(@name)"/>
         </call-template>
         <call-template name="wqt:array-setter">
            <with-param name="name" select="'OrderByAllowedProperties'"/>
            <with-param name="values" select="c:member[@wqt:sortable[xcst:boolean(.)]]/xcst:name(@name)"/>
         </call-template>
         <apply-templates select="(for $d in (., $defs) return $d/@order-by-parameter-allowed)[1]" mode="wqt:setter"/>
         <apply-templates select="(for $d in (., $defs) return $d/@skip-parameter-allowed)[1]" mode="wqt:setter"/>
         <apply-templates select="(for $d in (., $defs) return $d/@top-parameter-allowed)[1]" mode="wqt:setter"/>
      </variable>
      <c:metadata name="{src:global-identifier('WebQueryToolkit.WebQueryable')}">
         <variable name="setters-str" select="string-join($setters, ', ')"/>
         <if test="$setters-str">
            <attribute name="value" select="$setters-str"/>
         </if>
      </c:metadata>
   </template>

   <template match="@wqt:order-by" mode="wqt:setter">
      <value-of select="'OrderBy', src:verbatim-string(string())" separator=" = "/>
   </template>

   <template match="@order-by-parameter-allowed" mode="wqt:setter">
      <value-of select="'OrderByParameterAllowed', src:boolean(xcst:boolean(.))" separator=" = "/>
   </template>

   <template match="@skip-parameter-allowed" mode="wqt:setter">
      <value-of select="'SkipParameterAllowed', src:boolean(xcst:boolean(.))" separator=" = "/>
   </template>

   <template match="@top-parameter-allowed" mode="wqt:setter">
      <value-of select="'TopParameterAllowed', src:boolean(xcst:boolean(.))" separator=" = "/>
   </template>

   <template match="@top" mode="wqt:setter">
      <value-of select="'Top', src:integer(xcst:integer(.))" separator=" = "/>
   </template>

   <template match="@top-max" mode="wqt:setter">
      <value-of select="'TopMax', src:integer(xcst:integer(.))" separator=" = "/>
   </template>

   <template name="wqt:array-setter">
      <param name="name" as="xs:string" required="yes"/>
      <param name="values" as="item()*" required="yes"/>

      <if test="not(empty($values))">
         <variable name="strings" select="for $v in $values return src:verbatim-string($v)"/>
         <value-of select="$name, concat('new[] { ', string-join($strings, ', '), ' }')" separator=" = "/>
      </if>
   </template>

</stylesheet>
