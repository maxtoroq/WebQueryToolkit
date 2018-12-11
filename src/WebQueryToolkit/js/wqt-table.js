// Copyright 2018 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

(function ($) {
   'use strict';

   function filterable($table) {

      var $filtersRow = $table.find('> thead > tr.filters');
      var $form = $filtersRow.find('form');

      if (!$form.length) {
         return;
      }

      var formId = $form.attr('id');
      var $ctrl = $controls();

      $form.submit(submit);
      $ctrl.filter('select').change(apply);
      $ctrl.filter(':checkbox, :radio').click(apply);
      $form.find('button.clear').click(clear);

      function $controls() {
         return $(':input[form="' + formId + '"]');
      }

      function submit() {

         $controls()
            .filter(function () { return !$(this).val(); })
            .prop('disabled', true);

         return true;
      }

      function apply() {
         $(this.form).submit();
      }

      function clear() {
         $controls().each(function () { clearValue($(this)); });
         apply.apply(this);
      }

      function clearValue($control) {

         if ($control.is('.wqt-filter')) {
            return;
         }

         if ($control.is(':checkbox') || $control.is(':radio')) {
            $control.prop('checked', false);
         } else {
            $control.val('');
         }
      }
   }

   $.fn.wqtable = function () {

      return this.each(function () {

         var $this = $(this);

         filterable($this);
      });
   };

})(jQuery);
