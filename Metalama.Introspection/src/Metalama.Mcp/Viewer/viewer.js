// Metalama result viewer. Renders the JSON form of a result (result.schema.json) as LINQPad-style property tables and grids.
// It runs in three ways:
// - served by the result server of metalama-mcp on localhost, with the data embedded; drill-down fetches ../open, and the
//   query of the page can be edited and run again with ../query;
// - as an MCP App inside the host, which sends the data and forwards drill-down calls to the server;
// - as a saved snapshot, with the data embedded and no drill-down beyond it.
// Every value comes from the analysed code, so the DOM is built with textContent and attribute setters only.
( function () {
  'use strict';

  var app = document.getElementById( 'app' );
  var live = false;
  var served = false;

  // In the MCP App, what the host announced in its ui/initialize result. Drill-down needs serverTools, which says that the
  // host forwards tools/call to the server; openLinks lets the view open the served page in the browser instead.
  var hostCapabilities = {};
  var toolCallsRefused = false;
  var nextRequestId = 1;
  var pending = {};

  function el( tag, className, text ) {
    var element = document.createElement( tag );
    if ( className ) { element.className = className; }
    if ( text !== undefined && text !== null ) { element.textContent = String( text ); }
    return element;
  }

  // A self-contained page that has the workspace root and an editor link template turns each location into a link that opens
  // the file at the line. The MCP App does not, because its frame cannot open other applications.
  var editor = null;
  var root = null;

  function editorHref( location ) {
    if ( live || !editor || !root ) { return null; }
    var match = /^(.*):(\d+)$/.exec( location );
    if ( !match ) { return null; }
    var path = match[1].replace( /\\/g, '/' );
    if ( !/^([A-Za-z]:)?\//.test( path ) ) { path = root.replace( /\\/g, '/' ).replace( /\/$/, '' ) + '/' + path; }
    // The path in URI form: every segment is encoded, except a drive letter, and the path starts with a slash.
    path = path.split( '/' ).map( function ( segment, i ) { return i === 0 && /^[A-Za-z]:$/.test( segment ) ? segment : encodeURIComponent( segment ); } ).join( '/' );
    if ( path.charAt( 0 ) !== '/' ) { path = '/' + path; }
    return editor.split( '{path}' ).join( path ).split( '{line}' ).join( match[2] );
  }

  // A location that opens in the editor is a small source icon, with the location in its tooltip. Otherwise it stays text,
  // so that it is not lost.
  // The icon is a template of the page, which the HTML parser puts in the SVG namespace, so the page names no URL.
  function sourceIcon() {
    return document.getElementById( 'source-icon' ).content.firstElementChild.cloneNode( true );
  }

  function locationElement( node, className ) {
    var href = editorHref( node.location );
    if ( !href ) { return el( 'span', className, node.location ); }
    var link = el( 'a', 'source' );
    link.href = href;
    link.title = 'Open ' + node.location + ' in the editor';
    link.setAttribute( 'aria-label', link.title );
    link.appendChild( sourceIcon() );
    link.addEventListener( 'click', function ( event ) { event.stopPropagation(); } );
    return link;
  }

  function copy( text ) {
    try {
      var result = navigator.clipboard.writeText( text );
      if ( result && result.catch ) { result.catch( function () { } ); }
    } catch ( e ) { /* The clipboard is not available. */ }
  }

  function countText( node ) {
    if ( node.count === null || node.count === undefined ) { return '? items'; }
    return node.count === 1 ? '1 item' : node.count + ' items';
  }

  function toolButton( label, title, onClick ) {
    var button = el( 'button', 'tool', label );
    button.type = 'button';
    button.title = title;
    button.addEventListener( 'click', function ( event ) { event.stopPropagation(); onClick(); } );
    return button;
  }

  function tools( node ) {
    var span = el( 'span', 'tools' );
    if ( node.link ) { span.appendChild( toolButton( 'link', 'Copy the link: ' + node.link, function () { copy( node.link ); } ) ); }
    if ( node.expression ) { span.appendChild( toolButton( 'C#', 'Copy the C# expression: ' + node.expression, function () { copy( node.expression ); } ) ); }
    return span;
  }

  function collapsible( table, caption ) {
    caption.tabIndex = 0;
    var toggle = function () { table.classList.toggle( 'collapsed' ); };
    caption.addEventListener( 'click', toggle );
    caption.addEventListener( 'keydown', function ( event ) { if ( event.key === 'Enter' || event.key === ' ' ) { event.preventDefault(); toggle(); } } );
  }

  // In the MCP App and on a served page, a collapsed value with a link can be opened in place through the server.
  function canOpen() { return served || ( live && !!hostCapabilities.serverTools && !toolCallsRefused ); }

  function openable( element, node ) {
    if ( !canOpen() || !node.href ) {
      if ( node.link ) { element.title = node.link; }
      return element;
    }
    var button = el( 'button', 'link' );
    button.type = 'button';
    button.title = 'Open ' + node.link;
    button.appendChild( element );
    button.addEventListener( 'click', function () {
      var placeholder = el( 'span', 'muted', 'Loading…' );
      button.replaceWith( placeholder );
      openLink( node.href, null ).then(
        function ( result ) { placeholder.replaceWith( render( result, true ) ); },
        function ( error ) { placeholder.replaceWith( el( 'span', 'error', error && error.message ? error.message : String( error ) ) ); } );
    } );
    return button;
  }

  function summary( node ) {
    var span = el( 'span', 'summary' );
    if ( node.kind === 'collection' ) {
      span.textContent = node.count === 0 ? '[]' : '[' + ( node.count === null || node.count === undefined ? '?' : node.count ) + ']';
    } else {
      span.textContent = node.declaration && node.link ? node.link : node.display;
      if ( node.transformed ) { span.appendChild( el( 'span', 'muted', ' [transformed]' ) ); }
    }
    var result = openable( span, node );
    if ( node.location ) {
      var wrapper = el( 'span' );
      wrapper.appendChild( result );
      wrapper.appendChild( locationElement( node, 'location' ) );
      return wrapper;
    }
    return result;
  }

  function isExpandable( node ) {
    return !!node && ( ( node.kind === 'object' && node.properties ) || ( node.kind === 'collection' && node.items ) );
  }

  // As in LINQPad, a value nested in a table is shown collapsed, and expands in place from the data already in the page.
  function renderNested( node ) {
    if ( !isExpandable( node ) ) { return render( node ); }
    var wrapper = el( 'span', 'nested' );
    var toggle = el( 'button', 'toggle', '▸' );
    toggle.type = 'button';
    toggle.title = 'Expand';
    toggle.setAttribute( 'aria-expanded', 'false' );
    var collapsed = summary( node );
    var expanded = null;
    toggle.addEventListener( 'click', function () {
      var open = toggle.getAttribute( 'aria-expanded' ) !== 'true';
      if ( open && !expanded ) { expanded = render( node, true ); wrapper.appendChild( expanded ); }
      toggle.setAttribute( 'aria-expanded', String( open ) );
      toggle.textContent = open ? '▾' : '▸';
      toggle.title = open ? 'Collapse' : 'Expand';
      collapsed.hidden = open;
      if ( expanded ) { expanded.hidden = !open; }
      notifySize();
    } );
    wrapper.appendChild( toggle );
    wrapper.appendChild( collapsed );
    return wrapper;
  }

  function sortKey( node ) {
    if ( !node ) { return ''; }
    switch ( node.kind ) {
      case 'scalar': return node.type === 'number' ? Number( node.value ) : ( node.value === null ? '' : String( node.value ).toLowerCase() );
      case 'object': return String( node.declaration && node.link ? node.link : node.display ).toLowerCase();
      case 'collection': return node.count === null || node.count === undefined ? -1 : node.count;
      case 'text': return String( node.text ).toLowerCase();
      default: return '';
    }
  }

  function filterText( node ) {
    if ( !node ) { return ''; }
    switch ( node.kind ) {
      case 'scalar': return node.value === null ? 'null' : String( node.value );
      case 'object': return String( node.link || '' ) + ' ' + String( node.display || '' ) + ' ' + String( node.location || '' );
      case 'text': return String( node.text );
      case 'error': return String( node.message );
      default: return '';
    }
  }

  function caption( text, node, extra ) {
    var element = el( 'caption' );
    element.appendChild( document.createTextNode( text ) );
    if ( extra ) { element.appendChild( el( 'span', 'count', extra ) ); }
    if ( node.location ) { element.appendChild( locationElement( node, 'count' ) ); }
    element.appendChild( tools( node ) );
    return element;
  }

  // A property table starts collapsed to its header, which summarizes the object; a click expands it.
  function propertyTable( node, open ) {
    var table = el( 'table', open ? 'node properties' : 'node properties collapsed' );
    var title = node.display && node.display !== node.type ? node.type + '  ' + node.display : node.type;
    var cap = caption( title, node, null );
    table.appendChild( cap );
    collapsible( table, cap );
    var body = el( 'tbody' );
    node.properties.forEach( function ( property ) {
      var row = el( 'tr' );
      row.appendChild( el( 'th', null, property.name ) );
      var cell = el( 'td' );
      cell.appendChild( renderNested( property.value ) );
      row.appendChild( cell );
      body.appendChild( row );
    } );
    table.appendChild( body );
    return table;
  }

  function isGrid( items ) {
    if ( items.length === 0 ) { return false; }
    var type = items[0].type;
    return items.every( function ( item ) { return item.kind === 'object' && item.properties && item.type === type; } );
  }

  function collectionTable( node ) {
    var items = node.items;
    var table = el( 'table', 'node collection' );
    var extra = countText( node ) + ( node.truncated ? ', showing ' + items.length : '' );
    var cap = caption( node.type, node, extra );
    table.appendChild( cap );
    collapsible( table, cap );

    var grid = isGrid( items );
    var columns = [];
    if ( grid ) {
      items.forEach( function ( item ) {
        item.properties.forEach( function ( property ) { if ( columns.indexOf( property.name ) < 0 ) { columns.push( property.name ); } } );
      } );
    }

    var rows = items.map( function ( item ) {
      var row = el( 'tr' );
      var values = [];
      if ( grid ) {
        columns.forEach( function ( column ) {
          var property = item.properties.filter( function ( p ) { return p.name === column; } )[0];
          var value = property ? property.value : null;
          values.push( value );
          var cell = el( 'td' );
          if ( value ) { cell.appendChild( renderNested( value ) ); }
          row.appendChild( cell );
        } );
      } else {
        values.push( item );
        var cell = el( 'td' );
        cell.appendChild( renderNested( item ) );
        row.appendChild( cell );
      }
      row.dataset.filter = values.map( filterText ).join( ' ' ).toLowerCase();
      return { element: row, values: values };
    } );

    var head = el( 'thead' );
    if ( items.length > 5 ) {
      var filterRow = el( 'tr' );
      var filterCell = el( 'td' );
      filterCell.colSpan = Math.max( columns.length, 1 );
      var input = el( 'input', 'filter' );
      input.type = 'search';
      input.placeholder = 'Filter';
      input.setAttribute( 'aria-label', 'Filter rows' );
      input.addEventListener( 'input', function () {
        var text = input.value.toLowerCase();
        rows.forEach( function ( row ) { row.element.hidden = text.length > 0 && row.element.dataset.filter.indexOf( text ) < 0; } );
      } );
      filterCell.appendChild( input );
      filterRow.appendChild( filterCell );
      head.appendChild( filterRow );
    }

    var body = el( 'tbody' );

    if ( grid ) {
      var headerRow = el( 'tr' );
      columns.forEach( function ( column, index ) {
        var th = el( 'th', 'sortable', column );
        th.tabIndex = 0;
        th.title = 'Sort by ' + column;
        var sort = function () {
          var ascending = !th.classList.contains( 'sorted-asc' );
          headerRow.querySelectorAll( 'th' ).forEach( function ( other ) { other.classList.remove( 'sorted-asc', 'sorted-desc' ); } );
          th.classList.add( ascending ? 'sorted-asc' : 'sorted-desc' );
          rows.sort( function ( a, b ) {
            var x = sortKey( a.values[index] ), y = sortKey( b.values[index] );
            return ( x < y ? -1 : x > y ? 1 : 0 ) * ( ascending ? 1 : -1 );
          } );
          rows.forEach( function ( row ) { body.appendChild( row.element ); } );
        };
        th.addEventListener( 'click', sort );
        th.addEventListener( 'keydown', function ( event ) { if ( event.key === 'Enter' ) { sort(); } } );
        headerRow.appendChild( th );
      } );
      head.appendChild( headerRow );
    }

    table.appendChild( head );
    rows.forEach( function ( row ) { body.appendChild( row.element ); } );
    table.appendChild( body );

    if ( node.truncated ) {
      var foot = el( 'tfoot' );
      var footRow = el( 'tr' );
      var footCell = el( 'td', 'footer', items.length + ' of ' + ( node.count === null || node.count === undefined ? '?' : node.count ) + ' shown' );
      footCell.colSpan = Math.max( columns.length, 1 );
      if ( canOpen() && node.href ) {
        footCell.appendChild( toolButton( 'Show more', 'Open the collection with more items', function () {
          openLink( node.href, items.length * 4 ).then( function ( result ) { table.replaceWith( render( result ) ); } );
        } ) );
      }
      footRow.appendChild( footCell );
      foot.appendChild( footRow );
      table.appendChild( foot );
    }

    return table;
  }

  function groupingTable( node ) {
    var table = el( 'table', 'node grouping' );
    var key = node.key && node.key.kind === 'scalar' ? ( node.key.value === null ? 'null' : node.key.value ) : filterText( node.key );
    var cap = caption( String( key ), node, countText( node.items ) );
    table.appendChild( cap );
    collapsible( table, cap );
    var body = el( 'tbody' );
    var row = el( 'tr' );
    var cell = el( 'td' );
    cell.appendChild( node.items.items ? collectionTable( node.items ) : summary( node.items ) );
    row.appendChild( cell );
    body.appendChild( row );
    table.appendChild( body );
    return table;
  }

  function render( node, open ) {
    if ( !node ) { return el( 'span', 'null', 'null' ); }
    switch ( node.kind ) {
      case 'scalar':
        if ( node.type === 'null' ) { return el( 'span', 'null', 'null' ); }
        return el( 'span', node.type, node.value === '' ? '""' : node.value );
      case 'object':
        return node.properties ? propertyTable( node, open ) : summary( node );
      case 'collection':
        return node.items ? collectionTable( node ) : summary( node );
      case 'grouping':
        return groupingTable( node );
      case 'error':
        return el( 'span', 'error', '‹error: ' + node.exception + ': ' + node.message + '›' );
      case 'text':
        var span = el( 'span' );
        ( node.segments || [] ).forEach( function ( segment ) {
          span.appendChild( segment.node ? summary( segment.node ) : document.createTextNode( segment.text || '' ) );
        } );
        return span;
      default:
        return el( 'span', 'muted', JSON.stringify( node ) );
    }
  }

  function show( document_ ) {
    if ( !document_ || document_.schema !== 1 ) {
      app.replaceChildren( el( 'p', 'error', 'This result has an unknown format.' ) );
      return;
    }
    var titleElement = document.getElementById( 'title' );
    titleElement.textContent = document_.title || '';
    document.getElementById( 'base' ).textContent = document_.base ? 'base: ' + document_.base : '';
    editor = typeof document_.editor === 'string' ? document_.editor : null;
    root = typeof document_.root === 'string' ? document_.root : null;
    served = !live && document_.served === true;

    var toolbar = document.getElementById( 'toolbar' );
    toolbar.querySelectorAll( '.action' ).forEach( function ( action ) { action.remove(); } );

    if ( served && typeof document_.query === 'string' ) {
      titleElement.replaceChildren( queryEditor( document_.query, typeof document_.workspace === 'string' ? document_.workspace : null ) );
    }

    if ( served ) {
      // The server sends the same page without the served marker as an attachment, which works without the server.
      var save = el( 'a', 'action', 'Save' );
      save.href = '?download=1';
      save.title = 'Save a snapshot of this page. Values that were not loaded cannot be expanded in the snapshot.';
      toolbar.appendChild( save );
    } else if ( live && typeof document_.pageUrl === 'string' ) {
      // The same result on the result server, where every value expands on demand.
      toolbar.appendChild( pageAction( document_.pageUrl ) );
    }

    app.replaceChildren( render( document_.result ) );
    notifySize();
  }

  // On a served page, the query can be edited and run again. The result has its own page, so the browser history keeps both.
  function queryEditor( code, workspace ) {
    var form = el( 'form', 'query' );
    var text = el( 'textarea', 'query-code' );
    text.value = code;
    text.spellcheck = false;
    text.setAttribute( 'aria-label', 'Query' );
    text.rows = 1;

    // The box grows with its text, up to the height that the style sheet allows.
    function fit() { text.style.height = 'auto'; text.style.height = text.scrollHeight + 2 + 'px'; }
    text.addEventListener( 'input', fit );
    window.addEventListener( 'resize', fit );
    window.requestAnimationFrame( fit );
    var run = el( 'button', 'action run', 'Run' );
    run.type = 'submit';
    run.title = 'Run the query (Ctrl+Enter)';
    var error = el( 'pre', 'error query-error' );
    error.hidden = true;

    function submit() {
      if ( run.disabled ) { return; }
      run.disabled = true;
      run.textContent = 'Running…';
      error.hidden = true;
      fetch( '../query', {
        method: 'POST',
        credentials: 'same-origin',
        cache: 'no-store',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify( { code: text.value, workspace: workspace } )
      } ).then( function ( response ) {
        if ( !response.ok ) {
          return response.text().then( function ( message ) { throw new Error( message || 'The server returned ' + response.status + '.' ); } );
        }
        return response.json().then( function ( result ) { window.location.assign( result.url ); } );
      }, function () {
        throw new Error( 'The server is not running any more. Ask the agent to run the query again.' );
      } ).catch( function ( e ) {
        error.textContent = e && e.message ? e.message : String( e );
        error.hidden = false;
        run.disabled = false;
        run.textContent = 'Run';
      } );
    }

    form.addEventListener( 'submit', function ( event ) { event.preventDefault(); submit(); } );
    text.addEventListener( 'keydown', function ( event ) {
      if ( event.key === 'Enter' && ( event.ctrlKey || event.metaKey ) ) { event.preventDefault(); submit(); }
    } );
    form.appendChild( text );
    form.appendChild( run );
    form.appendChild( error );
    return form;
  }

  function pageAction( url ) {
    if ( hostCapabilities.openLinks ) {
      var open = el( 'button', 'action', 'Open in browser' );
      open.type = 'button';
      open.title = 'Open this result in your browser, where every value can be expanded.';
      open.addEventListener( 'click', function () {
        request( 'ui/open-link', { url: url } ).catch( function ( error ) { open.title = String( error ); open.classList.add( 'failed' ); } );
      } );
      return open;
    }
    // The host cannot open links: the user can paste it in a browser.
    var copyLink = el( 'button', 'action', 'Copy browser link' );
    copyLink.type = 'button';
    copyLink.title = 'Copy the link of this result, to open it in your browser, where every value can be expanded.';
    copyLink.addEventListener( 'click', function () { copy( url ); } );
    return copyLink;
  }

  // MCP Apps: JSON-RPC 2.0 over postMessage with the host.
  function send( message ) { window.parent.postMessage( message, '*' ); }

  function request( method, params ) {
    var id = nextRequestId++;
    send( { jsonrpc: '2.0', id: id, method: method, params: params } );
    return new Promise( function ( resolve, reject ) { pending[id] = { resolve: resolve, reject: reject }; } );
  }

  function openLink( href, maxItems ) {
    if ( served ) {
      // The page is at <token>/r/<id>, and the server follows links at <token>/open.
      var url = '../open?link=' + encodeURIComponent( href ) + ( maxItems ? '&maxItems=' + encodeURIComponent( String( maxItems ) ) : '' );
      return fetch( url, { credentials: 'same-origin', cache: 'no-store' } ).then( function ( response ) {
        if ( !response.ok ) {
          return response.text().then( function ( text ) { throw new Error( text || 'The server returned ' + response.status + '.' ); } );
        }
        return response.json().then( function ( document_ ) { return document_.result; } );
      }, function () {
        throw new Error( 'The server is not running any more. Ask the agent to run the query again.' );
      } );
    }

    var args = { link: href, format: 'json' };
    if ( maxItems ) { args.maxItems = maxItems; }
    return request( 'tools/call', { name: 'open', arguments: args } ).then( function ( result ) {
      if ( result && result.isError ) {
        var content = ( result.content || [] ).map( function ( c ) { return c.text || ''; } ).join( '\n' );
        throw new Error( content || 'The server returned an error.' );
      }
      if ( !result || !result.structuredContent ) { throw new Error( 'The server returned no structured result.' ); }
      return result.structuredContent.result;
    }, function ( error ) {
      // The host refused the call itself, although it announced serverTools: stop offering drill-down in this view.
      toolCallsRefused = true;
      throw new Error( 'This host does not let the view load more data (' + error.message + '). Use "Open in browser", or ask the agent for the html format.' );
    } );
  }

  function notifySize() {
    if ( live ) {
      send( { jsonrpc: '2.0', method: 'ui/notifications/size-changed', params: { width: document.documentElement.scrollWidth, height: document.documentElement.scrollHeight } } );
    }
  }

  window.addEventListener( 'message', function ( event ) {
    if ( event.source !== window.parent ) { return; }
    var message = event.data;
    if ( !message || message.jsonrpc !== '2.0' ) { return; }

    if ( message.id !== undefined && !message.method && pending[message.id] ) {
      var callbacks = pending[message.id];
      delete pending[message.id];
      if ( message.error ) { callbacks.reject( new Error( message.error.message || 'Error' ) ); } else { callbacks.resolve( message.result ); }
      return;
    }

    switch ( message.method ) {
      case 'ui/notifications/tool-result':
        if ( message.params && message.params.structuredContent ) {
          show( message.params.structuredContent );
        } else {
          var text = ( message.params && message.params.content || [] ).map( function ( c ) { return c.text || ''; } ).join( '\n' );
          app.replaceChildren( el( 'pre', message.params && message.params.isError ? 'error' : 'mono', text ) );
        }
        break;
      case 'ui/notifications/tool-cancelled':
        app.replaceChildren( el( 'p', 'muted', 'The call was cancelled.' ) );
        break;
      case 'ui/resource-teardown':
        if ( message.id !== undefined ) { send( { jsonrpc: '2.0', id: message.id, result: {} } ); }
        break;
    }
  } );

  var data = document.getElementById( 'metalama-data' );

  if ( data ) {
    try {
      show( JSON.parse( data.textContent ) );
    } catch ( e ) {
      app.replaceChildren( el( 'p', 'error', 'Cannot read the embedded result: ' + e.message ) );
    }
  } else if ( window.parent !== window ) {
    live = true;
    request( 'ui/initialize', { protocolVersion: '2026-01-26', appInfo: { name: 'Metalama viewer', version: '1.0.0' }, appCapabilities: {} } )
      .then( function ( result ) {
        hostCapabilities = result && result.hostCapabilities || {};
        send( { jsonrpc: '2.0', method: 'ui/notifications/initialized', params: {} } );
      } );
    if ( window.ResizeObserver ) { new ResizeObserver( notifySize ).observe( document.body ); }
  } else {
    app.replaceChildren( el( 'p', 'muted', 'This page shows Metalama query results. Open the link returned by the html format of the query tool.' ) );
  }
} )();
