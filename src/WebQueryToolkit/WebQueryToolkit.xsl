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
   xmlns:xcst="http://maxtoroq.github.io/XCST/grammar"
   xmlns:code="http://maxtoroq.github.io/XCST/code"
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
      <variable name="setters" as="element()*">
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
      <code:attribute>
         <code:type-reference name="WebQueryable" namespace="WebQueryToolkit"/>
         <code:initializer>
            <sequence select="$setters"/>
         </code:initializer>
      </code:attribute>
   </template>

   <template match="@wqt:order-by" mode="wqt:setter">
      <code:member-initializer name="OrderBy">
         <code:string verbatim="true">
            <value-of select="."/>
         </code:string>
      </code:member-initializer>
   </template>

   <template match="@order-by-parameter-allowed" mode="wqt:setter">
      <code:member-initializer name="OrderByParameterAllowed">
         <code:bool value="{xcst:boolean(.)}"/>
      </code:member-initializer>
   </template>

   <template match="@skip-parameter-allowed" mode="wqt:setter">
      <code:member-initializer name="SkipParameterAllowed">
         <code:bool value="{xcst:boolean(.)}"/>
      </code:member-initializer>
   </template>

   <template match="@top-parameter-allowed" mode="wqt:setter">
      <code:member-initializer name="TopParameterAllowed">
         <code:bool value="{xcst:boolean(.)}"/>
      </code:member-initializer>
   </template>

   <template match="@top" mode="wqt:setter">
      <code:member-initializer name="Top">
         <code:int value="{xcst:integer(.)}"/>
      </code:member-initializer>
   </template>

   <template match="@top-max" mode="wqt:setter">
      <code:member-initializer name="TopMax">
         <code:int value="{xcst:integer(.)}"/>
      </code:member-initializer>
   </template>

   <template name="wqt:array-setter">
      <param name="name" as="xs:string" required="yes"/>
      <param name="values" as="item()*" required="yes"/>

      <if test="not(empty($values))">
         <code:member-initializer name="{$name}">
            <code:new-array>
               <code:type-reference name="String" namespace="System"/>
               <code:collection-initializer>
                  <for-each select="$values">
                     <code:string verbatim="true">
                        <value-of select="."/>
                     </code:string>
                  </for-each>
               </code:collection-initializer>
            </code:new-array>
         </code:member-initializer>
      </if>
   </template>

</stylesheet>
